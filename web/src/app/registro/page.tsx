import { redirect } from "next/navigation";

export default function RegistroPage() {
  redirect("/entrar?mode=sign-up");
}
