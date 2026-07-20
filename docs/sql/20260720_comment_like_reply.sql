-- Apply manually if `dotnet ef database update` is locked by running API.
-- Comment likes + reply (parent_comment_id)

ALTER TABLE comments
  ADD COLUMN IF NOT EXISTS parent_comment_id uuid NULL;

CREATE TABLE IF NOT EXISTS comment_likes (
  id uuid PRIMARY KEY,
  comment_id uuid NOT NULL,
  user_id uuid NOT NULL,
  created_at timestamp with time zone NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_comment_likes_comment_id_user_id
  ON comment_likes (comment_id, user_id);

CREATE INDEX IF NOT EXISTS ix_comment_likes_user_id
  ON comment_likes (user_id);

CREATE INDEX IF NOT EXISTS ix_comments_parent_comment_id
  ON comments (parent_comment_id);

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_comments_comments_parent_comment_id'
  ) THEN
    ALTER TABLE comments
      ADD CONSTRAINT fk_comments_comments_parent_comment_id
      FOREIGN KEY (parent_comment_id) REFERENCES comments(id) ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_comment_likes_comments_comment_id'
  ) THEN
    ALTER TABLE comment_likes
      ADD CONSTRAINT fk_comment_likes_comments_comment_id
      FOREIGN KEY (comment_id) REFERENCES comments(id) ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'fk_comment_likes_users_user_id'
  ) THEN
    ALTER TABLE comment_likes
      ADD CONSTRAINT fk_comment_likes_users_user_id
      FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE;
  END IF;
END $$;
