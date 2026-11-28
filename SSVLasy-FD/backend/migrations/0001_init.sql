CREATE EXTENSION IF NOT EXISTS postgis;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TYPE user_role AS ENUM ('super_admin', 'chef');
CREATE TYPE campaign_state AS ENUM ('draft', 'scheduled', 'running', 'paused', 'finished');
CREATE TYPE cipher_kind AS ENUM ('morse', 'caesar', 'atbash', 'vigenere', 'polybius', 'a1z26', 'reversed');
CREATE TYPE activity_status AS ENUM ('draft', 'published', 'unlocked');
CREATE TYPE progress_status AS ENUM ('open', 'awaiting_choice', 'free_retry', 'skip_only', 'solved', 'skipped');
CREATE TYPE attempt_outcome AS ENUM ('correct', 'wrong', 'expired');

CREATE TABLE users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email text NOT NULL,
    display_name text NOT NULL,
    password_hash text NOT NULL,
    role user_role NOT NULL,
    active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX users_email_unique ON users (lower(email));

CREATE TABLE teams (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    chef_id uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    name text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (chef_id, name)
);

CREATE TABLE members (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    team_id uuid NOT NULL REFERENCES teams (id) ON DELETE CASCADE,
    pseudonym text NOT NULL,
    access_digest char(64) NOT NULL UNIQUE,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (team_id, pseudonym)
);

CREATE TABLE campaigns (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id uuid NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    name text NOT NULL,
    start_date date NOT NULL,
    days smallint NOT NULL CHECK (days BETWEEN 3 AND 11),
    timezone text NOT NULL DEFAULT 'Indian/Antananarivo',
    state campaign_state NOT NULL DEFAULT 'draft',
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE activities (
    id uuid PRIMARY KEY,
    campaign_id uuid NOT NULL REFERENCES campaigns (id) ON DELETE CASCADE,
    title text NOT NULL,
    day smallint NOT NULL CHECK (day BETWEEN 1 AND 11),
    slot_start time NOT NULL,
    slot_end time NOT NULL,
    points integer NOT NULL CHECK (points BETWEEN 1 AND 1000),
    cipher cipher_kind NOT NULL,
    cipher_shift smallint CHECK (cipher_shift BETWEEN 1 AND 25),
    cipher_key text,
    encoded_text text NOT NULL,
    answer_digest char(64) NOT NULL,
    solution_sealed bytea NOT NULL,
    status activity_status NOT NULL DEFAULT 'draft',
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (slot_start < slot_end)
);
CREATE INDEX activities_campaign_day ON activities (campaign_id, day);

CREATE TABLE team_activity (
    activity_id uuid NOT NULL REFERENCES activities (id) ON DELETE CASCADE,
    team_id uuid NOT NULL REFERENCES teams (id) ON DELETE CASCADE,
    status progress_status NOT NULL DEFAULT 'open',
    attempts_used smallint NOT NULL DEFAULT 0 CHECK (attempts_used >= 0),
    running boolean NOT NULL DEFAULT false,
    deadline_at timestamptz,
    awarded_points integer NOT NULL DEFAULT 0,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (activity_id, team_id)
);

CREATE TABLE attempts (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activity_id uuid NOT NULL REFERENCES activities (id) ON DELETE CASCADE,
    team_id uuid NOT NULL REFERENCES teams (id) ON DELETE CASCADE,
    number smallint NOT NULL,
    outcome attempt_outcome NOT NULL,
    recorded_at timestamptz NOT NULL DEFAULT now()
);

CREATE FUNCTION enforce_user_quota() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    current_count integer;
    max_allowed integer;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('user_quota_' || NEW.role::text));
    SELECT count(*) INTO current_count FROM users WHERE role = NEW.role AND id <> NEW.id;
    max_allowed := CASE NEW.role WHEN 'super_admin' THEN 10 ELSE 100 END;
    IF current_count >= max_allowed THEN
        RAISE EXCEPTION 'QUOTA_USERS_%', upper(NEW.role::text) USING ERRCODE = 'P0001';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER users_quota BEFORE INSERT OR UPDATE OF role ON users
    FOR EACH ROW EXECUTE FUNCTION enforce_user_quota();

CREATE FUNCTION enforce_team_quota() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    current_count integer;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('team_quota_' || NEW.chef_id::text));
    SELECT count(*) INTO current_count FROM teams WHERE chef_id = NEW.chef_id AND id <> NEW.id;
    IF current_count >= 3 THEN
        RAISE EXCEPTION 'QUOTA_TEAMS_PER_CHEF' USING ERRCODE = 'P0001';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER teams_quota BEFORE INSERT OR UPDATE OF chef_id ON teams
    FOR EACH ROW EXECUTE FUNCTION enforce_team_quota();

CREATE FUNCTION enforce_member_quota() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    current_count integer;
BEGIN
    PERFORM pg_advisory_xact_lock(hashtext('member_quota_' || NEW.team_id::text));
    SELECT count(*) INTO current_count FROM members WHERE team_id = NEW.team_id AND id <> NEW.id;
    IF current_count >= 5 THEN
        RAISE EXCEPTION 'QUOTA_MEMBERS_PER_TEAM' USING ERRCODE = 'P0001';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER members_quota BEFORE INSERT OR UPDATE OF team_id ON members
    FOR EACH ROW EXECUTE FUNCTION enforce_member_quota();

CREATE FUNCTION enforce_owner_is_chef() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM users
        WHERE id = (to_jsonb(NEW) ->> TG_ARGV[0])::uuid AND role = 'chef' AND active
    ) THEN
        RAISE EXCEPTION 'OWNER_NOT_CHEF' USING ERRCODE = 'P0001';
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER teams_owner_is_chef BEFORE INSERT OR UPDATE OF chef_id ON teams
    FOR EACH ROW EXECUTE FUNCTION enforce_owner_is_chef('chef_id');

CREATE TRIGGER campaigns_owner_is_chef BEFORE INSERT OR UPDATE OF owner_id ON campaigns
    FOR EACH ROW EXECUTE FUNCTION enforce_owner_is_chef('owner_id');

CREATE FUNCTION guard_last_super_admin() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.role = 'super_admin' AND OLD.active
        AND (TG_OP = 'DELETE' OR NEW.role <> 'super_admin' OR NOT NEW.active)
        AND NOT EXISTS (
            SELECT 1 FROM users WHERE role = 'super_admin' AND active AND id <> OLD.id
        ) THEN
        RAISE EXCEPTION 'LAST_SUPER_ADMIN' USING ERRCODE = 'P0001';
    END IF;
    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    END IF;
    RETURN NEW;
END $$;

CREATE TRIGGER users_keep_super_admin BEFORE DELETE OR UPDATE OF role, active ON users
    FOR EACH ROW EXECUTE FUNCTION guard_last_super_admin();

CREATE FUNCTION app_role() RETURNS text LANGUAGE sql STABLE AS $$
    SELECT coalesce(current_setting('app.role', true), '')
$$;

CREATE FUNCTION app_user_id() RETURNS uuid LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('app.user_id', true), '')::uuid
$$;

CREATE FUNCTION app_chef_id() RETURNS uuid LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('app.chef_id', true), '')::uuid
$$;

CREATE FUNCTION app_team_id() RETURNS uuid LANGUAGE sql STABLE AS $$
    SELECT nullif(current_setting('app.team_id', true), '')::uuid
$$;

CREATE FUNCTION can_manage_chef_data(chef uuid) RETURNS boolean LANGUAGE sql STABLE AS $$
    SELECT app_role() IN ('system', 'super_admin') OR (app_role() = 'chef' AND app_user_id() = chef)
$$;

CREATE FUNCTION can_access_chef_data(chef uuid) RETURNS boolean LANGUAGE sql STABLE AS $$
    SELECT can_manage_chef_data(chef) OR (app_role() = 'member' AND app_chef_id() = chef)
$$;

CREATE FUNCTION can_manage_team(team uuid) RETURNS boolean LANGUAGE sql STABLE AS $$
    SELECT EXISTS (SELECT 1 FROM teams WHERE teams.id = team AND can_manage_chef_data(teams.chef_id))
$$;

CREATE FUNCTION can_manage_campaign(campaign uuid) RETURNS boolean LANGUAGE sql STABLE AS $$
    SELECT EXISTS (
        SELECT 1 FROM campaigns WHERE campaigns.id = campaign AND can_manage_chef_data(campaigns.owner_id)
    )
$$;

DO $$
DECLARE
    protected_table text;
BEGIN
    FOREACH protected_table IN ARRAY ARRAY[
        'users', 'teams', 'members', 'campaigns', 'activities', 'team_activity', 'attempts'
    ] LOOP
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', protected_table);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', protected_table);
    END LOOP;
END $$;

CREATE POLICY users_read ON users FOR SELECT
    USING (app_role() IN ('system', 'super_admin') OR id = app_user_id());
CREATE POLICY users_write ON users FOR ALL
    USING (app_role() IN ('system', 'super_admin'))
    WITH CHECK (app_role() IN ('system', 'super_admin'));

CREATE POLICY teams_read ON teams FOR SELECT
    USING (can_access_chef_data(chef_id) AND (app_role() <> 'member' OR id = app_team_id()));
CREATE POLICY teams_write ON teams FOR ALL
    USING (can_manage_chef_data(chef_id)) WITH CHECK (can_manage_chef_data(chef_id));

CREATE POLICY members_read ON members FOR SELECT
    USING (team_id = app_team_id() OR can_manage_team(team_id));
CREATE POLICY members_write ON members FOR ALL
    USING (can_manage_team(team_id)) WITH CHECK (can_manage_team(team_id));

CREATE POLICY campaigns_read ON campaigns FOR SELECT USING (can_access_chef_data(owner_id));
CREATE POLICY campaigns_write ON campaigns FOR ALL
    USING (can_manage_chef_data(owner_id)) WITH CHECK (can_manage_chef_data(owner_id));

CREATE POLICY activities_read ON activities FOR SELECT
    USING (
        EXISTS (SELECT 1 FROM campaigns WHERE campaigns.id = activities.campaign_id)
        AND (app_role() <> 'member' OR status <> 'draft')
    );
CREATE POLICY activities_write ON activities FOR ALL
    USING (can_manage_campaign(campaign_id)) WITH CHECK (can_manage_campaign(campaign_id));

CREATE POLICY team_activity_access ON team_activity FOR ALL
    USING (team_id = app_team_id() OR can_manage_team(team_id))
    WITH CHECK (team_id = app_team_id() OR can_manage_team(team_id));

CREATE POLICY attempts_access ON attempts FOR ALL
    USING (team_id = app_team_id() OR can_manage_team(team_id))
    WITH CHECK (team_id = app_team_id() OR can_manage_team(team_id));
