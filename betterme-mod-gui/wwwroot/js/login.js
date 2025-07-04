
async function loginUser() {
  const username = document.getElementById('txtUsername').value.trim();
  const password = document.getElementById('pwdPassword').value;

  if (!username || !password) {
    alert('Rellena usuario y contraseña');
    return;
  }

  try {
    const res = await fetch('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
    });

    if (res.status === 401) {
      alert('Credenciales inválidas');
      return;
    }
    if (!res.ok) {
      alert('Error del servidor');
      return;
    }

    const { accessToken } = await res.json();

    document.cookie = `accessToken=${accessToken}; path=/; SameSite=Lax`;
    window.location.href = '/PendingReports';
  } catch (err) {
    console.error(err);
    alert('No se pudo conectar al servidor');
  }
}
