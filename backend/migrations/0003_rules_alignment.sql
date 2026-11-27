ALTER TABLE campaigns DROP CONSTRAINT IF EXISTS campaigns_days_check;
ALTER TABLE campaigns ADD CONSTRAINT campaigns_days_check CHECK (days BETWEEN 1 AND 11);

CREATE FUNCTION enforce_activities_per_day() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    current_count integer;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('activities_day_' || NEW.campaign_id::text || '_' || NEW.day::text));
    SELECT count(*) INTO current_count
    FROM activities
    WHERE campaign_id = NEW.campaign_id AND day = NEW.day AND id <> NEW.id;
    IF current_count >= 7 THEN
        RAISE EXCEPTION 'QUOTA_ACTIVITIES_PER_DAY' USING ERRCODE = 'P0001';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER activities_per_day BEFORE INSERT OR UPDATE OF campaign_id, day ON activities
    FOR EACH ROW EXECUTE FUNCTION enforce_activities_per_day();
