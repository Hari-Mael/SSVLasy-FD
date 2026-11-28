ALTER TABLE campaigns ADD COLUMN camp_type text NOT NULL DEFAULT 'tselatra'
    CHECK (camp_type IN ('tselatra', 'lehibe', 'feno'));
ALTER TABLE campaigns ADD COLUMN current_day smallint NOT NULL DEFAULT 1 CHECK (current_day >= 1);
ALTER TABLE campaigns ADD COLUMN weather text NOT NULL DEFAULT 'chaleur'
    CHECK (weather IN ('chaleur', 'pluvieux', 'venteux', 'chaud_humide', 'venteux_humide', 'chaud_sec'));
ALTER TABLE members ADD COLUMN energy smallint NOT NULL DEFAULT 100 CHECK (energy BETWEEN 0 AND 100);

CREATE TABLE team_camp_state (
    team_id uuid PRIMARY KEY REFERENCES teams (id) ON DELETE CASCADE,
    wood integer NOT NULL DEFAULT 0 CHECK (wood >= 0),
    water integer NOT NULL DEFAULT 0 CHECK (water >= 0),
    food integer NOT NULL DEFAULT 0 CHECK (food >= 0),
    missed_meal_days integer NOT NULL DEFAULT 0 CHECK (missed_meal_days >= 0),
    blocked_since timestamptz,
    sick boolean NOT NULL DEFAULT false
);

CREATE TABLE team_day_actions (
    team_id uuid NOT NULL REFERENCES teams (id) ON DELETE CASCADE,
    campaign_id uuid NOT NULL REFERENCES campaigns (id) ON DELETE CASCADE,
    day smallint NOT NULL,
    resource text NOT NULL CHECK (resource IN ('wood', 'water', 'food')),
    PRIMARY KEY (team_id, day, resource)
);

ALTER TABLE team_camp_state ENABLE ROW LEVEL SECURITY;
ALTER TABLE team_day_actions ENABLE ROW LEVEL SECURITY;

CREATE POLICY team_camp_state_access ON team_camp_state FOR ALL
    USING (
        EXISTS (SELECT 1 FROM teams t WHERE t.id = team_camp_state.team_id AND can_manage_chef_data(t.chef_id))
        OR (app_role() = 'member' AND team_camp_state.team_id = app_team_id())
    )
    WITH CHECK (
        EXISTS (SELECT 1 FROM teams t WHERE t.id = team_camp_state.team_id AND can_manage_chef_data(t.chef_id))
        OR (app_role() = 'member' AND team_camp_state.team_id = app_team_id())
    );

CREATE POLICY team_day_actions_access ON team_day_actions FOR ALL
    USING (
        EXISTS (SELECT 1 FROM teams t WHERE t.id = team_day_actions.team_id AND can_manage_chef_data(t.chef_id))
        OR (app_role() = 'member' AND team_day_actions.team_id = app_team_id())
    )
    WITH CHECK (
        EXISTS (SELECT 1 FROM teams t WHERE t.id = team_day_actions.team_id AND can_manage_chef_data(t.chef_id))
        OR (app_role() = 'member' AND team_day_actions.team_id = app_team_id())
    );
