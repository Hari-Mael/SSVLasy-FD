ALTER TABLE members ADD COLUMN login text;
ALTER TABLE members ADD COLUMN password_hash text;
UPDATE members SET login = lower(pseudonym) || '-' || substr(replace(id::text, '-', ''), 1, 6)
    WHERE login IS NULL;
ALTER TABLE members ALTER COLUMN login SET NOT NULL;
ALTER TABLE members ADD CONSTRAINT members_login_key UNIQUE (login);
ALTER TABLE members ALTER COLUMN access_digest DROP NOT NULL;
