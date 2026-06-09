"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/hooks/useAuth";
import { Button } from "@/components/ui/Button";

export default function LoginPage() {
  const router = useRouter();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      router.replace("/dashboard");
    } catch {
      setError("Invalid email or password.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-6">
      <div className="w-full max-w-sm rounded-xl bg-surface p-8 shadow-[var(--shadow-lg)]">
        <div className="mb-6 flex items-center gap-2 font-bold text-primary">
          <span aria-hidden className="inline-block h-6 w-6 rounded-pill bg-accent" />
          Ghế Đầy
        </div>
        <h1 className="text-2xl font-bold text-primary">Sign in</h1>
        <p className="mt-1 text-sm text-secondary">Manage your bookings and conversations.</p>

        <form className="mt-6 flex flex-col gap-4" onSubmit={onSubmit}>
          <label className="flex flex-col gap-1 text-sm text-secondary">
            Email
            <input
              type="email"
              required
              autoComplete="username"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="rounded-md bg-elevated px-3 py-2 text-primary shadow-[var(--shadow-inset)] placeholder:text-tertiary"
              placeholder="owner@example.com"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm text-secondary">
            Password
            <input
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="rounded-md bg-elevated px-3 py-2 text-primary shadow-[var(--shadow-inset)]"
            />
          </label>

          {error && (
            <p role="alert" className="text-sm text-error">
              {error}
            </p>
          )}

          <Button type="submit" disabled={submitting} className="mt-2 w-full">
            {submitting ? "Signing in…" : "Sign in"}
          </Button>
        </form>
      </div>
    </main>
  );
}
