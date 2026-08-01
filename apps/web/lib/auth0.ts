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
