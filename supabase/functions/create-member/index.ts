// Crée le compte d'un membre (pseudonyme + mot de passe) à la demande d'un chef.
// Exécuté côté serveur : la clé service_role n'est jamais exposée au client.

import { createClient } from "npm:@supabase/supabase-js@2";

const MEMBER_EMAIL_DOMAIN = "members.sahavola.app";
const MIN_PASSWORD_LENGTH = 8;

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

// "Renard  Rouge " -> "renard-rouge"
export function loginFromPseudonym(pseudonym: string): string {
  return pseudonym
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase()
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9-]/g, "");
}

Deno.serve(async (request: Request): Promise<Response> => {
  if (request.method !== "POST") {
    return jsonResponse({ error: "Méthode non autorisée" }, 405);
  }

  const token = request.headers.get("Authorization")?.replace("Bearer ", "");
  if (!token) {
    return jsonResponse({ error: "Authentification requise" }, 401);
  }

  const url = Deno.env.get("SUPABASE_URL")!;
  const anonKey = Deno.env.get("SUPABASE_ANON_KEY")!;
  const serviceKey = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;

  // 1) Qui appelle ? Le jeton de l'appelant, pas la clé service.
  const caller = createClient(url, anonKey, {
    global: { headers: { Authorization: `Bearer ${token}` } },
  });
  const { data: userData, error: userError } = await caller.auth.getUser(token);
  if (userError || !userData.user) {
    return jsonResponse({ error: "Authentification invalide" }, 401);
  }
  const { data: profile } = await caller
    .from("profiles")
    .select("id, role, active")
    .eq("id", userData.user.id)
    .single();
  if (!profile || !profile.active || profile.role !== "chef") {
    return jsonResponse({ error: "Réservé aux chefs" }, 403);
  }

  // 2) Données reçues, validées côté serveur.
  let body: Record<string, unknown>;
  try {
    body = await request.json();
  } catch {
    return jsonResponse({ error: "Corps de requête invalide" }, 400);
  }
  const pseudonym = String(body.pseudonym ?? "").trim();
  const password = String(body.password ?? "");
  const parentName = String(body.parent_name ?? "").trim();
  const parentContact = String(body.parent_contact ?? "").trim();
  const login = loginFromPseudonym(pseudonym);

  if (!login || login.length > 40) {
    return jsonResponse({ error: "Pseudonyme invalide" }, 422);
  }
  if (password.length < MIN_PASSWORD_LENGTH) {
    return jsonResponse({ error: `Mot de passe : au moins ${MIN_PASSWORD_LENGTH} caractères` }, 422);
  }
  if (!parentName || !parentContact) {
    return jsonResponse({ error: "Nom et contact du parent obligatoires" }, 422);
  }

  // 3) Création du compte avec la clé service (serveur uniquement).
  const admin = createClient(url, serviceKey, {
    auth: { autoRefreshToken: false, persistSession: false },
  });
  const email = `${login}@${MEMBER_EMAIL_DOMAIN}`;
  const { data: created, error: createError } = await admin.auth.admin.createUser({
    email,
    password,
    email_confirm: true,
  });
  if (createError || !created.user) {
    return jsonResponse({ error: "Pseudonyme déjà utilisé" }, 409);
  }

  // 4) Profil. En cas d'échec, on supprime le compte pour ne rien laisser d'incohérent.
  const { error: profileError } = await admin.from("profiles").insert({
    id: created.user.id,
    login,
    display_name: pseudonym.replace(/\s+/g, " "),
    role: "membre",
    chef_id: userData.user.id,
    parent_name: parentName,
    parent_contact: parentContact,
  });
  if (profileError) {
    await admin.auth.admin.deleteUser(created.user.id);
    const quota = profileError.message.includes("QUOTA_MEMBRE_PAR_CHEF");
    return jsonResponse(
      { error: quota ? "Maximum de 15 membres atteint" : "Création impossible" },
      quota ? 409 : 500,
    );
  }

  return jsonResponse({ id: created.user.id, login }, 201);
});
