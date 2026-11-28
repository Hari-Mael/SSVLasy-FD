import { createClient } from "npm:@supabase/supabase-js@2.45.4";

const MEMBER_EMAIL_DOMAIN = "members.sahavola.app";
const MIN_PASSWORD_BYTES = 8;
const MAX_PASSWORD_BYTES = 72;
const MAX_NAME_LENGTH = 40;
const MAX_PARENT_LENGTH = 120;
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function json(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

export function loginFromPseudonym(pseudonym: string): string {
  return pseudonym
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase()
    .replace(/[\s_]+/g, "-")
    .replace(/[^a-z0-9-]/g, "")
    .replace(/-+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function bearerToken(request: Request): string | null {
  const header = request.headers.get("Authorization") ?? "";
  return header.startsWith("Bearer ") ? header.slice(7) : null;
}

function mapRpcError(message: string): { status: number; error: string } {
  if (message.includes("QUOTA_MEMBERS_PER_TEAM")) {
    return { status: 409, error: "Équipe complète (5 membres maximum)" };
  }
  if (message.includes("TEAM_NOT_OWNED")) {
    return { status: 403, error: "Équipe inconnue" };
  }
  if (message.includes("profiles_login_key")) {
    return { status: 409, error: "Pseudonyme déjà utilisé" };
  }
  if (message.includes("GUARDIAN_REQUIRED")) {
    return { status: 422, error: "Responsable légal obligatoire" };
  }
  return { status: 500, error: "Création impossible" };
}

Deno.serve(async (request: Request): Promise<Response> => {
  if (request.method !== "POST") {
    return json({ error: "Méthode non autorisée" }, 405);
  }

  const token = bearerToken(request);
  if (!token) {
    return json({ error: "Authentification requise" }, 401);
  }

  const admin = createClient(
    Deno.env.get("SUPABASE_URL")!,
    Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
    { auth: { autoRefreshToken: false, persistSession: false } },
  );

  const { data: userData, error: userError } = await admin.auth.getUser(token);
  if (userError || !userData.user) {
    return json({ error: "Authentification invalide" }, 401);
  }
  const chefId = userData.user.id;

  const { data: chef } = await admin
    .from("profiles")
    .select("role, active")
    .eq("id", chefId)
    .maybeSingle();
  if (!chef || !chef.active || chef.role !== "chef") {
    return json({ error: "Réservé aux chefs" }, 403);
  }

  let body: Record<string, unknown>;
  try {
    body = await request.json();
  } catch {
    return json({ error: "Corps de requête invalide" }, 400);
  }

  const teamId = String(body.team_id ?? "");
  const pseudonym = String(body.pseudonym ?? "").trim().replace(/\s+/g, " ");
  const password = String(body.password ?? "");
  const parentName = String(body.parent_name ?? "").trim();
  const parentContact = String(body.parent_contact ?? "").trim();
  const login = loginFromPseudonym(pseudonym);
  const passwordBytes = new TextEncoder().encode(password).length;

  if (!UUID_PATTERN.test(teamId)) {
    return json({ error: "Équipe invalide" }, 422);
  }
  if (!login || login.length > MAX_NAME_LENGTH || pseudonym.length > MAX_NAME_LENGTH) {
    return json({ error: "Pseudonyme invalide" }, 422);
  }
  if (passwordBytes < MIN_PASSWORD_BYTES || passwordBytes > MAX_PASSWORD_BYTES) {
    return json({ error: "Mot de passe : entre 8 et 72 octets" }, 422);
  }
  if (
    !parentName || !parentContact ||
    parentName.length > MAX_PARENT_LENGTH || parentContact.length > MAX_PARENT_LENGTH
  ) {
    return json({ error: "Nom et contact du responsable obligatoires" }, 422);
  }

  const { data: created, error: createError } = await admin.auth.admin.createUser({
    email: `${login}@${MEMBER_EMAIL_DOMAIN}`,
    password,
    email_confirm: true,
  });
  if (createError || !created.user) {
    const taken = /already|registered|exists/i.test(createError?.message ?? "");
    return json(
      { error: taken ? "Pseudonyme déjà utilisé" : "Création impossible" },
      taken ? 409 : 422,
    );
  }

  const { error: rpcError } = await admin.rpc("register_member", {
    p_chef_id: chefId,
    p_team_id: teamId,
    p_user_id: created.user.id,
    p_login: login,
    p_display_name: pseudonym,
    p_parent_name: parentName,
    p_parent_contact: parentContact,
  });
  if (rpcError) {
    await admin.auth.admin.deleteUser(created.user.id);
    const mapped = mapRpcError(rpcError.message);
    return json({ error: mapped.error }, mapped.status);
  }

  return json({ id: created.user.id, login }, 201);
});
