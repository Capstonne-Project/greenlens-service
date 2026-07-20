-- ============================================================
-- Chạy trong DBeaver trước khi dotnet run.
-- Drop tất cả bảng + history để EF tạo lại từ đầu.
-- ============================================================

DO $$ 
DECLARE r RECORD;
BEGIN
    FOR r IN (
        SELECT constraint_name, table_name 
        FROM information_schema.table_constraints 
        WHERE constraint_type = 'FOREIGN KEY' 
          AND table_schema = 'public'
    ) LOOP
        EXECUTE 'ALTER TABLE public.' || quote_ident(r.table_name) || 
                ' DROP CONSTRAINT ' || quote_ident(r.constraint_name);
    END LOOP;
END $$;

DROP TABLE IF EXISTS report_waste_tags CASCADE;
DROP TABLE IF EXISTS waste_tags CASCADE;
DROP TABLE IF EXISTS report_media CASCADE;
DROP TABLE IF EXISTS report_assignments CASCADE;
DROP TABLE IF EXISTS reports CASCADE;
DROP TABLE IF EXISTS otp_codes CASCADE;
DROP TABLE IF EXISTS refresh_tokens CASCADE;
DROP TABLE IF EXISTS environmental_teams CASCADE;
DROP TABLE IF EXISTS local_offices CASCADE;
DROP TABLE IF EXISTS departments CASCADE;
DROP TABLE IF EXISTS wards CASCADE;
DROP TABLE IF EXISTS provinces CASCADE;
DROP TABLE IF EXISTS administrative_units CASCADE;
DROP TABLE IF EXISTS administrative_regions CASCADE;
DROP TABLE IF EXISTS pollution_categories CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
