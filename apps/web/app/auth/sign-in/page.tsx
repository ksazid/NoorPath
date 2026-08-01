import Link from "next/link";

type Props = { searchParams: Promise<{ returnUrl?: string }> };

export default async function SignInPage({ searchParams }: Props) {
  const requestedReturnUrl = (await searchParams).returnUrl;
  const returnUrl =
    requestedReturnUrl?.startsWith("/") && !requestedReturnUrl.startsWith("//")
      ? requestedReturnUrl
      : "/account";

  return (
    <main className="auth-page" id="main-content">
      <Link className="auth-brand" href="/" aria-label="NoorPath home">
        NoorPath
      </Link>
      <section className="auth-card" aria-labelledby="sign-in-title">
        <p className="auth-eyebrow">Your secure journey</p>
        <h1 id="sign-in-title">Sign in to NoorPath</h1>
        <p className="auth-intro">
          Access your journey or approved workspace. NoorPath never asks you to
          create or share a password.
        </p>
        <div className="auth-actions">
          <a
            className="auth-primary"
            href={`/api/auth/sign-in?method=phone&returnUrl=${encodeURIComponent(returnUrl)}`}
          >
            Continue with phone OTP
          </a>
          <a
            className="auth-secondary"
            href={`/api/auth/sign-in?method=google&returnUrl=${encodeURIComponent(returnUrl)}`}
          >
            <span aria-hidden="true" className="google-mark">
              G
            </span>
            Continue with Google
          </a>
        </div>
        <p className="auth-help">
          Phone codes and Google authentication are completed securely by the
          configured identity service. Codes and provider tokens are never
          stored by NoorPath.
        </p>
      </section>
      <p className="auth-support">
        Having trouble signing in? <Link href="/#support">Contact support</Link>
      </p>
    </main>
  );
}
