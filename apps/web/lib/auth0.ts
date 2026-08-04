import { Auth0Client } from "@auth0/nextjs-auth0/server";

export function isAuth0Configured() {
  return [
    process.env.AUTH0_DOMAIN,
    process.env.AUTH0_CLIENT_ID,
    process.env.AUTH0_CLIENT_SECRET,
    process.env.AUTH0_SECRET,
  ].every(Boolean);
}

const client = isAuth0Configured()
  ? new Auth0Client({
      authorizationParameters: {
        audience: process.env.AUTH0_AUDIENCE,
      },
      enableAccessTokenEndpoint: false,
    })
  : null;

export function getAuth0Client() {
  return client;
}

export async function getAuth0ApiAccessToken() {
  const auth0 = getAuth0Client();
  if (!auth0) return null;

  const session = await auth0.getSession();
  if (!session) return null;

  if (session.accessToken) return session.accessToken;

  const refreshed = await auth0.getAccessToken();
  return refreshed.token || null;
}
