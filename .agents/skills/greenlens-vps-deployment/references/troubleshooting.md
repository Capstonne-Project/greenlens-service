# Troubleshooting — Greenlens VPS Deployment

## Where the logs live

| Component | Path / Command |
|---|---|
| API (Serilog file sink) | `/opt/greenlens/logs/greenlens-*.log` |
| API (stdout, last 1h) | `docker compose logs --tail=500 api` |
| Postgres | `docker compose logs --tail=200 postgres` |
| Redis | `docker compose logs --tail=200 redis` |
| Cloudflared | `journalctl -u cloudflared -n 200 --no-pager` |
| SSH/login | `journalctl -u ssh -n 100` |
| ufw drops | `sudo dmesg | grep -i ufw | tail` |
| fail2ban | `sudo fail2ban-client status sshd` |

## Top issues + fixes

### 1. GitHub Actions deploy fails: "permission denied (publickey)"

**Cause:** `VPS_SSH_KEY` secret sai format hoặc public key chưa add cho `appuser`.

**Fix:**
```bash
# Trên máy local — lấy đúng private key
cat ~/.ssh/id_ed25519
# Copy TOÀN BỘ (kể cả -----BEGIN... -----END...) vào VPS_SSH_KEY secret

# Trên VPS — verify public key
sudo cat /home/appuser/.ssh/authorized_keys
# Phải có line khớp với ~/.ssh/id_ed25519.pub
```

### 2. `curl https://<tunnel-url>/health` → 502 Bad Gateway

**Cause:** Tunnel chạy nhưng API trong Docker chưa healthy, hoặc API bind sai interface.

**Diagnose:**
```bash
ssh appuser@<VPS_IP>

# A) API có healthy không?
docker compose ps
# Nếu không "(healthy)" → check log
docker compose logs --tail=100 api

# B) API có bind đúng localhost:8080?
sudo ss -tlnp | grep 8080
# → 127.0.0.1:8080 hoặc 0.0.0.0:8080 (cả 2 đều OK cho tunnel)

# C) Cloudflared route đúng?
sudo cat /etc/cloudflared/config.yml 2>/dev/null || \
  echo "Token mode — check ingress in CF dashboard"

# D) Test thủ công localhost
curl -v http://localhost:8080/health
# → nếu fail ở đây, vấn đề ở API, không ở tunnel
```

### 3. Postgres không khởi động được: "database files are incompatible"

**Cause:** Đã chạy Postgres 16 trước, giờ image là 18 → data dir mismatch.

**Fix:**
```bash
# Backup data
docker compose exec postgres pg_dumpall -U greenlens_app > /opt/greenlens/backups/predowngrade.sql

# Nuke volume
docker compose down
docker volume rm greenlens_postgres_data

# Khởi tạo lại
docker compose --env-file .env.production up -d postgres
sleep 10
docker compose --env-file .env.production run --rm migrator

# Restore
cat /opt/greenlens/backups/predowngrade.sql \
  | docker compose exec -T postgres psql -U greenlens_app
```

### 4. Cloudflared: `error="Unauthorized"` hoặc tunnel status "DOWN"

**Cause:** Token sai/expired, hoặc tunnel đã bị delete trên CF dashboard.

**Fix:**
```bash
sudo systemctl stop cloudflared
sudo cloudflared service uninstall

# Lấy token mới từ CF dashboard → install lại
sudo cloudflared service install eyJh...
sudo systemctl status cloudflared
```

### 5. Disk full

```bash
# Xem ai ngốn disk
df -h
sudo du -sh /var/lib/docker/* 2>/dev/null | sort -h
sudo du -sh /opt/greenlens/*

# Dọn docker
docker system prune -af --volumes      # CẨN THẬN — xoá cả volume not in use
docker image prune -af

# Dọn log
sudo journalctl --vacuum-time=7d
sudo truncate -s 0 /opt/greenlens/logs/*.log    # nếu log file quá to
```

### 6. Migration job timeout / locked

**Cause:** Một deploy trước fail giữa chừng, để lại `__EFMigrationsHistory` lock row.

**Fix:**
```bash
docker compose exec postgres psql -U greenlens_app -d greenlens \
  -c "SELECT * FROM __ef_migrations_history ORDER BY migration_id DESC LIMIT 5;"

# Nếu có pending lock — manual rollback:
docker compose exec api dotnet ef database update <previousMigration>
# rồi rerun migrator
```

### 7. "Health check failed" trong GitHub Actions

Workflow đã có auto-rollback. Sau khi rollback:
```bash
ssh appuser@<VPS_IP>
docker compose logs --tail=200 api > /tmp/failed-deploy.log
# Đọc log để hiểu vì sao crash. Push fix lên main → workflow chạy lại.
```

## Rollback thủ công (nếu CI/CD không tự rollback được)

```bash
ssh appuser@<VPS_IP>
cd /opt/greenlens

# Liệt kê các tag image còn lưu
docker images ghcr.io/<owner>/greenlens-api

# Chọn tag muốn rollback (sha-<hash>)
export IMAGE_TAG=sha-abc123def
export GITHUB_OWNER=<owner>
sudo docker compose --env-file .env.production up -d api

# Verify
curl http://localhost:8080/health
```

## Performance baseline

NFR target (OVERVIEW.md): p95 < 2s @ 5K CCU.

Quick load test từ máy local:
```bash
# Cài hey: https://github.com/rakyll/hey
hey -n 1000 -c 50 https://<tunnel-url>/api/v1/reports
# → check P95 line. Nếu > 2s với 50 concurrent → có vấn đề.
```

Trên VPS check:
```bash
# CPU/RAM
htop

# Postgres slow queries
docker compose exec postgres psql -U greenlens_app -d greenlens \
  -c "SELECT pid, now()-query_start as duration, query FROM pg_stat_activity WHERE state='active' AND now()-query_start > interval '500ms';"

# Redis hits
docker compose exec redis redis-cli --no-auth-warning -a "$REDIS_PASSWORD" info stats | grep keyspace
```

## Backup + Restore

### Backup (đặt cron hàng ngày)

```bash
# /opt/greenlens/scripts/backup.sh
#!/bin/bash
TS=$(date +%Y%m%d-%H%M%S)
docker compose -f /opt/greenlens/docker-compose.yml exec -T postgres \
    pg_dump -U greenlens_app greenlens \
    | gzip > /opt/greenlens/backups/greenlens-${TS}.sql.gz

# Giữ 14 ngày
find /opt/greenlens/backups -name "*.sql.gz" -mtime +14 -delete
```

Add cron:
```bash
sudo crontab -e -u appuser
# 0 3 * * * /opt/greenlens/scripts/backup.sh
```

Off-VPS storage: rsync vào R2:
```bash
# Bonus: backup vào R2 bucket
aws s3 sync /opt/greenlens/backups s3://greenlens-backups/ \
    --endpoint-url https://<account>.r2.cloudflarestorage.com
```

### Restore

```bash
gunzip -c /opt/greenlens/backups/greenlens-20260509-030000.sql.gz \
  | docker compose exec -T postgres psql -U greenlens_app -d greenlens
```
