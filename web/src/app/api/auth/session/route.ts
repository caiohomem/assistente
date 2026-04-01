import { auth, clerkClient } from "@clerk/nextjs/server";
import { NextResponse } from "next/server";

export async function GET() {
  const { userId, sessionClaims, getToken } = await auth();

  if (!userId) {
    return NextResponse.json({
      authenticated: false,
      csrfToken: "",
      accessToken: null,
      expiresAtUnix: null,
      user: null,
    });
  }

  const accessToken = await getToken();
  const client = await clerkClient();
  const user = await client.users.getUser(userId);

  const email =
    user.primaryEmailAddress?.emailAddress ??
    (typeof sessionClaims?.email === "string" ? sessionClaims.email : null) ??
    (typeof sessionClaims?.["email_address"] === "string" ? sessionClaims["email_address"] : null);
  const givenName =
    user.firstName ??
    (typeof sessionClaims?.["first_name"] === "string" ? sessionClaims["first_name"] : null);
  const familyName =
    user.lastName ??
    (typeof sessionClaims?.["last_name"] === "string" ? sessionClaims["last_name"] : null);
  const fullName =
    user.fullName ??
    (typeof sessionClaims?.["full_name"] === "string"
      ? sessionClaims["full_name"]
      : [givenName, familyName].filter(Boolean).join(" ") || null);

  return NextResponse.json({
    authenticated: true,
    csrfToken: "",
    accessToken,
    expiresAtUnix: null,
    user: {
      sub: userId,
      email,
      name: fullName,
      givenName,
      familyName,
    },
  });
}
