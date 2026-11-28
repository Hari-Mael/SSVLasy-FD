\set ON_ERROR_STOP 1
create or replace function pg_temp.expect_error(stmt text, msg text) returns void language plpgsql as $$
begin
    execute stmt;
    raise exception 'ATTENDU ERREUR "%" mais succès : %', msg, stmt;
exception when others then
    if sqlerrm not like '%' || msg || '%' then
        raise exception 'MAUVAISE ERREUR (attendu "%") : % sur %', msg, sqlerrm, stmt;
    end if;
    raise notice 'OK erreur attendue : %', msg;
end $$;

create or replace function pg_temp.check_count(label text, actual bigint, expected bigint) returns void language plpgsql as $$
begin
    if actual <> expected then
        raise exception 'ECHEC % : attendu %, obtenu %', label, expected, actual;
    end if;
    raise notice 'OK % = %', label, actual;
end $$;

create or replace function pg_temp.as_user(uid uuid) returns void language plpgsql as $$
begin
    perform set_config('request.jwt.claim.sub', coalesce(uid::text, ''), false);
    execute case when uid is null then 'set role anon' else 'set role authenticated' end;
end $$;

create or replace function pg_temp.as_admin() returns void language plpgsql as $$
begin
    reset role;
    perform set_config('request.jwt.claim.sub', '', false);
end $$;


create or replace function pg_temp.as_service() returns void language plpgsql as $f$
begin
    reset role;
    perform set_config('request.jwt.claim.sub', '', false);
    execute 'set role service_role';
end $f$;
\set C1 '00000000-0000-0000-0000-000000000002'
\set C2 '00000000-0000-0000-0000-000000000003'
\set M1 '00000000-0000-0000-0000-000000000011'
\set CH1 '00000000-0000-0000-0000-000000000031'
\set CH2 '00000000-0000-0000-0000-000000000032'
\set CH3 '00000000-0000-0000-0000-000000000033'
\set CH4 '00000000-0000-0000-0000-000000000034'
\set ACT1 '00000000-0000-0000-0000-000000000501'
\set ACT2 '00000000-0000-0000-0000-000000000502'

select pg_temp.as_admin();
insert into auth.users (id) values (:'CH1'), (:'CH2'), (:'CH3'), (:'CH4');

select pg_temp.as_user(:'C1');
select pg_temp.expect_error($q$select public.create_campaign('Fuseau faux', 'lehibe', 2, current_date + 3, 'Mars/Nulle_part')$q$, 'FUSEAU_INCONNU');
select pg_temp.expect_error($q$select public.create_campaign('Trop long', 'tselatra', 4, current_date + 3, 'Indian/Antananarivo')$q$, 'DUREE_INVALIDE');
select public.create_campaign('Camp planifié', 'lehibe', 3, current_date + 3, 'Indian/Antananarivo') as camp_id \gset
select pg_temp.check_count('chef crée une campagne', (select count(*) from public.campaigns where id = :'camp_id'), 1);
select pg_temp.expect_error(format($q$insert into public.campaigns (owner_id, name, camp_type, days, start_date) values (%L, 'Direct', 'feno', 2, current_date)$q$, :'C1'), 'permission denied');
select pg_temp.as_user(:'M1');
select pg_temp.expect_error($q$select public.create_campaign('Membre', 'feno', 2, current_date, 'Indian/Antananarivo')$q$, 'REAL_CHEF_REQUIRED');
select pg_temp.as_user(:'C1');
select pg_temp.expect_error(format($q$select public.transition_campaign(%L, 'running')$q$, :'camp_id'), 'TRANSITION_CAMPAGNE');
select pg_temp.expect_error(format($q$select public.transition_campaign(%L, 'scheduled')$q$, :'camp_id'), 'CHEFS_INSUFFICIENTS');

select pg_temp.as_admin();
insert into public.profiles (id, role, display_name) values
    (:'CH1', 'chef', 'Chef 3'), (:'CH2', 'chef', 'Chef 4'), (:'CH3', 'chef', 'Chef 5'), (:'CH4', 'chef', 'Chef 6');

select pg_temp.as_user(:'C1');
select public.transition_campaign(:'camp_id', 'scheduled');
select pg_temp.check_count('campagne planifiée', (select count(*) from public.campaigns where id = :'camp_id' and state = 'scheduled'), 1);
select public.create_campaign('Camp passé', 'lehibe', 2, date '2020-01-01', 'Indian/Antananarivo') as past_id \gset
select pg_temp.expect_error(format($q$select public.transition_campaign(%L, 'scheduled')$q$, :'past_id'), 'DATE_DEBUT_PASSEE');

select public.transition_campaign(:'camp_id', 'running');
select public.transition_campaign(:'camp_id', 'paused');
select public.transition_campaign(:'camp_id', 'running');
select public.transition_campaign(:'camp_id', 'finished');
select pg_temp.expect_error(format($q$select public.transition_campaign(%L, 'running')$q$, :'camp_id'), 'TRANSITION_CAMPAGNE');
select pg_temp.check_count('campagne terminée', (select count(*) from public.campaigns where id = :'camp_id' and state = 'finished'), 1);

select public.create_campaign('Camp actions', 'lehibe', 3, current_date + 3, 'Indian/Antananarivo') as act_camp_id \gset
select public.transition_campaign(:'act_camp_id', 'scheduled');
select public.transition_campaign(:'act_camp_id', 'running');

select pg_temp.as_service();
select public.insert_activity(:'ACT1', :'act_camp_id', 'Énigme un', 1, '09:00', '10:00', 10, 'caesar', 'KHOOR', repeat('a', 64), '\x00', 3, null);
select pg_temp.expect_error(format($q$select public.insert_activity('00000000-0000-0000-0000-000000000599', %L, 'Interdit', 1, '09:00', '10:00', 5, 'morse', 'x', repeat('b', 64), '\x00', null, null)$q$, :'camp_id'), 'CAMPAGNE_TERMINEE');
select pg_temp.as_admin();
select pg_temp.check_count('secret créé avec l activité', (select count(*) from public.activity_secrets where activity_id = :'ACT1'), 1);

select pg_temp.as_user(:'M1');
select pg_temp.expect_error(format($q$select public.publish_activity(%L)$q$, :'ACT1'), 'NON_AUTORISE');
select pg_temp.as_user(:'C2');
select pg_temp.expect_error(format($q$select public.publish_activity(%L)$q$, :'ACT1'), 'NON_AUTORISE');
select pg_temp.check_count('chef étranger ne voit pas les secrets', (select count(*) from public.activity_secrets where activity_id = :'ACT1'), 0);

select pg_temp.as_user(:'C1');
select public.publish_activity(:'ACT1');
select pg_temp.expect_error(format($q$select public.publish_activity(%L)$q$, :'ACT1'), 'TRANSITION_ACTIVITE');
select public.unlock_activity(:'ACT1');
select pg_temp.check_count('activité débloquée', (select count(*) from public.activities where id = :'ACT1' and status = 'unlocked'), 1);

select pg_temp.as_service();
select public.insert_activity(:'ACT2', :'act_camp_id', 'Énigme deux', 2, '09:00', '10:00', 10, 'atbash', 'ZYX', repeat('c', 64), '\x00', null, null);

select pg_temp.as_user(:'C1');
select public.publish_activity(:'ACT2');
select public.transition_campaign(:'act_camp_id', 'paused');
select pg_temp.expect_error(format($q$select public.unlock_activity(%L)$q$, :'ACT2'), 'CAMPAGNE_NON_EN_COURS');
select public.transition_campaign(:'act_camp_id', 'running');
select public.unlock_activity(:'ACT2');

select pg_temp.expect_error(format($q$update public.activities set title = 'Piraté' where id = %L$q$, :'ACT1'), 'permission denied');
select pg_temp.expect_error(format($q$insert into public.activity_secrets (activity_id, answer_digest, solution_sealed) values (%L, repeat('d', 64), '\x00')$q$, :'ACT2'), 'permission denied');
select pg_temp.check_count('chef voit les secrets de son camp', (select count(*) from public.activity_secrets where activity_id in (:'ACT1', :'ACT2')), 2);

select pg_temp.as_admin();
select 'TESTS CAMPAGNES ET ACTIVITES OK' as resultat;
