# Comptes des membres (décision 1)

## Principe
- Les chefs et le Super Admin : e-mail réel + mot de passe (Supabase Auth).
- Les membres : pseudonyme + mot de passe. Le pseudonyme devient l'identifiant
  (`renard-rouge`) et l'e-mail technique est `renard-rouge@members.sahavola.app`.
  Aucun e-mail n'est jamais envoyé à cette adresse.

## Mise en place (une seule fois, dans le tableau de bord Supabase)
1. **Authentication > Providers > Email** : désactiver « Confirm email » et
   les e-mails de récupération de mot de passe pour les membres.
2. **SQL Editor** : coller le contenu de `migrations/0001_member_accounts.sql`, puis exécuter.
3. **Edge Functions > Deploy a new function** : nom `create-member`, coller le
   contenu de `functions/create-member/index.ts`, puis déployer.
   La clé `service_role` est fournie automatiquement à la fonction : ne la copiez nulle part.

## Appel depuis le jeu (chef connecté)
POST `https://<projet>.supabase.co/functions/v1/create-member`
Authorization: Bearer <jeton du chef>
Corps JSON : { "pseudonym", "password", "parent_name", "parent_contact" }

## Connexion d'un membre
signInWithPassword(email = "<login>@members.sahavola.app", password)
