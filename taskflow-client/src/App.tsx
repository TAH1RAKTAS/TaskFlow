import { useCallback, useEffect, useState } from 'react'
import './App.css'
type Status = 'Başlamadı' | 'Devam Ediyor' | 'Tamamlandı'
type Task = { id: number; title: string; description: string; priority: string; status: Status; dueDate: string | null }
type View = 'tasks' | 'completed' | 'reminders' | 'settings'
const api = import.meta.env.VITE_API_URL ?? 'http://localhost:5070'
const dateValue = (date: string | null) => date?.slice(0, 10) ?? ''
function daysUntil(date: string | null) {
  if (!date) return null
  const today = new Date(); today.setHours(0, 0, 0, 0)
  return Math.round((new Date(`${date.slice(0, 10)}T00:00:00`).getTime() - today.getTime()) / 86400000)
}
function remainingLabel(date: string | null) {
  const days = daysUntil(date)
  if (days === null) return '—'
  if (days === 0) return 'Bugün'
  return days > 0 ? `${days} gün` : `${Math.abs(days)} gün geçti`
}
function remainingClass(date: string | null) {
  const days = daysUntil(date)
  if (days === null) return 'no-date'
  if (days <= 2) return 'urgent'
  if (days <= 7) return 'soon'
  return 'safe'
}

async function apiErrorMessage(response: Response, fallback: string) {
  const text = await response.text()
  if (!text) return fallback

  try {
    const payload = JSON.parse(text) as {
      message?: unknown
      errors?: Record<string, unknown>
    }
    const validationMessages = Object.values(payload.errors ?? {})
      .flatMap((value) => Array.isArray(value) ? value : [value])
      .filter((value): value is string => typeof value === 'string' && value.trim().length > 0)

    if (validationMessages.length > 0) return validationMessages.join(' ')
    if (typeof payload.message === 'string' && payload.message.trim()) return payload.message
    return fallback
  } catch {
    return text.trim() || fallback
  }
}

const isValidEmail = (value: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim())

function App() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isLoggedIn, setIsLoggedIn] = useState(() => Boolean(localStorage.getItem('token')))
  const [accountEmail, setAccountEmail] = useState(() => localStorage.getItem('accountEmail') ?? '')
  const [tasks, setTasks] = useState<Task[]>([])
  const [hasNextPage, setHasNextPage] = useState(false)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState('Orta')
  const [status, setStatus] = useState<Status>('Başlamadı')
  const [dueDate, setDueDate] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState('due_date')
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(5)
  const [view, setView] = useState<View>('tasks')
  const [isRegistering, setIsRegistering] = useState(false)
  const [message, setMessage] = useState('')
  const [taskMessage, setTaskMessage] = useState('')
  const [isEditorOpen, setIsEditorOpen] = useState(false)
  const [reminderTaskId, setReminderTaskId] = useState('')
  const [reminderEmail, setReminderEmail] = useState('')
  const [reminderDays, setReminderDays] = useState('1')
  const [reminders, setReminders] = useState<{ id: number; taskTitle: string; recipientEmail: string; daysBefore: number; isSent: boolean }[]>([])
  const resetForm = useCallback(() => { setEditingId(null); setTitle(''); setDescription(''); setPriority('Orta'); setStatus('Başlamadı'); setDueDate(''); setTaskMessage(''); setIsEditorOpen(false) }, [])
  const logout = useCallback(() => { localStorage.removeItem('token'); localStorage.removeItem('accountEmail'); setAccountEmail(''); setIsLoggedIn(false); setTasks([]); setHasNextPage(false); resetForm() }, [resetForm])
  const getTasks = useCallback(async () => {
    if (view === 'settings' || view === 'reminders') return
    const params = new URLSearchParams({ search, sort, page: String(page), pageSize: String(pageSize), status: view === 'completed' ? 'Tamamlandı' : 'active' })
    try {
      const response = await fetch(`${api}/Task?${params}`, { headers: { Authorization: `Bearer ${localStorage.getItem('token')}` } })
      if (response.status === 401) return logout()
      if (!response.ok) throw new Error()
      const data = await response.json(); setTasks(data.data?.items ?? []); setHasNextPage(data.data?.hasNextPage ?? false); setTaskMessage('')
    } catch { setTaskMessage('Görevler alınamadı. API’nin çalıştığından emin olun.') }
  }, [logout, page, pageSize, search, sort, view])
  const login = async () => {
    setMessage('')
    if (!isValidEmail(email)) return setMessage('Geçerli bir e-posta adresi girin.')
    if (!password) return setMessage('Şifre zorunludur.')
    try {
      const response = await fetch(`${api}/Auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }) })
      if (!response.ok) return setMessage(await apiErrorMessage(response, 'Giriş yapılamadı.'))
      const data = await response.text()
      const normalizedEmail = email.trim().toLowerCase()
      localStorage.setItem('token', data); localStorage.setItem('accountEmail', normalizedEmail); setAccountEmail(normalizedEmail); setIsLoggedIn(true); setPassword('')
    } catch { setMessage('Sunucuya bağlanılamadı.') }
  }
  const register = async () => {
    setMessage('')
    if (!isValidEmail(email)) return setMessage('Geçerli bir e-posta adresi girin.')
    if (password.length < 8) return setMessage('Şifre en az 8 karakter olmalıdır.')
    try {
      const response = await fetch(`${api}/Auth/register`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }) })
      if (!response.ok) return setMessage(await apiErrorMessage(response, 'Kayıt oluşturulamadı.'))
      setIsRegistering(false); setPassword(''); setMessage('Kayıt başarılı. Giriş yapabilirsin.')
    } catch { setMessage('Sunucuya bağlanılamadı.') }
  }
  const saveTask = async (id?: number) => {
    if (!title.trim() || !description.trim()) return setTaskMessage('Başlık ve açıklama boş bırakılamaz.')
    const response = await fetch(`${api}/Task${id ? `/${id}` : ''}`, { method: id ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token')}` }, body: JSON.stringify({ title: title.trim(), description: description.trim(), priority, status, dueDate: dueDate || null }) })
    if (!response.ok) return setTaskMessage(await apiErrorMessage(response, id ? 'Görev güncellenemedi.' : 'Görev oluşturulamadı.'))
    resetForm(); setPage(1); await getTasks()
  }
  const removeTask = async (id: number) => {
    const response = await fetch(`${api}/Task/${id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${localStorage.getItem('token')}` } })
    if (!response.ok) return setTaskMessage('Görev silinemedi.')
    if (tasks.length === 1 && page > 1) return setPage(page - 1)
    await getTasks()
  }
  const updateStatus = async (task: Task, next: Status) => {
    const response = await fetch(`${api}/Task/${task.id}/status`, { method: 'PATCH', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token')}` }, body: JSON.stringify({ status: next }) })
    if (!response.ok) {
      return setTaskMessage(await apiErrorMessage(response, 'Görev durumu güncellenemedi.'))
    }
    await getTasks()
  }
  const edit = (task: Task) => { setEditingId(task.id); setTitle(task.title); setDescription(task.description); setPriority(task.priority); setStatus(task.status); setDueDate(dateValue(task.dueDate)); setTaskMessage(''); setIsEditorOpen(true) }
  const changeView = (next: View) => { setView(next); setPage(1); resetForm() }
  const saveSettings = async (nextPageSize: number, nextSort: string) => {
    const response = await fetch(`${api}/settings`, { method: 'PUT', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token')}` }, body: JSON.stringify({ defaultPageSize: nextPageSize, defaultSort: nextSort }) })
    if (response.status === 401) logout()
  }
  const loadReminders = useCallback(async () => {
    const response = await fetch(`${api}/reminders`, { headers: { Authorization: `Bearer ${localStorage.getItem('token')}` } })
    if (response.status === 401) return logout()
    if (!response.ok) return
    const data = await response.json()
    setReminders(data.data ?? [])
  }, [logout])
  const createReminder = async () => {
    if (!reminderTaskId) return setTaskMessage('Hatırlatıcı için bir görev seçin.')
    if (!isValidEmail(reminderEmail)) return setTaskMessage('Geçerli bir e-posta adresi girin.')
    const days = Number(reminderDays)
    if (!Number.isInteger(days) || days < 0 || days > 365) return setTaskMessage('Gün sayısı 0 ile 365 arasında olmalıdır.')

    try {
      const response = await fetch(`${api}/reminders`, { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${localStorage.getItem('token')}` }, body: JSON.stringify({ taskItemId: Number(reminderTaskId), recipientEmail: reminderEmail.trim(), daysBefore: days }) })
      if (response.status === 401) return logout()
      if (!response.ok) return setTaskMessage(await apiErrorMessage(response, 'Hatırlatıcı kaydedilemedi.'))
      setTaskMessage('Hatırlatıcı planlandı. E-posta, belirlenen tarihte gönderilecek.'); setReminderTaskId(''); await loadReminders()
    } catch {
      setTaskMessage('Sunucuya bağlanılamadı. Hatırlatıcı kaydedilemedi.')
    }
  }
  useEffect(() => {
    if (!isLoggedIn) return
    const request = window.setTimeout(() => void getTasks(), 0)
    return () => window.clearTimeout(request)
  }, [getTasks, isLoggedIn])
  useEffect(() => {
    if (!isLoggedIn) return
    fetch(`${api}/settings`, { headers: { Authorization: `Bearer ${localStorage.getItem('token')}` } })
      .then(async (response) => {
        if (response.status === 401) return logout()
        if (!response.ok) return
        const data = await response.json()
        setPageSize(data.data.defaultPageSize)
        setSort(data.data.defaultSort)
      })
      .catch(() => undefined)
  }, [isLoggedIn, logout])
  useEffect(() => {
    if (!isLoggedIn) return
    const request = window.setTimeout(() => void loadReminders(), 0)
    return () => window.clearTimeout(request)
  }, [isLoggedIn, loadReminders])
  if (!isLoggedIn) return <div className="login-container"><div className="login-card"><h1>TaskFlow</h1><p>{isRegistering ? 'Yeni hesap oluştur' : 'Hesabına giriş yap'}</p><input type="email" placeholder="E-posta" value={email} onChange={(e) => setEmail(e.target.value)} /><input type="password" placeholder="Şifre" value={password} onChange={(e) => setPassword(e.target.value)} />{message && <p className={`auth-message ${message.startsWith('Kayıt başarılı') ? 'success' : ''}`}>{message}</p>}<button onClick={isRegistering ? register : login}>{isRegistering ? 'Kayıt Ol' : 'Giriş Yap'}</button><button className="switch-auth" onClick={() => { setIsRegistering(!isRegistering); setMessage('') }}>{isRegistering ? 'Zaten hesabın var mı? Giriş yap' : 'Hesabın yok mu? Kayıt ol'}</button></div></div>
  const sorting = <select value={sort} onChange={(e) => { setSort(e.target.value); setPage(1) }}><option value="due_date">Tarih (en yakın)</option><option value="due_date_desc">Tarih (en uzak)</option><option value="title">Başlık (A-Z)</option><option value="title_desc">Başlık (Z-A)</option><option value="priority">Öncelik (yüksek)</option><option value="priority_desc">Öncelik (düşük)</option><option value="status">Durum</option></select>
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand"><span className="brand-mark">✓</span>TaskFlow</div>
        <nav className="sidebar-nav">
          <button className={`nav-item ${view === 'tasks' ? 'active' : ''}`} onClick={() => changeView('tasks')}>▣ Görevlerim</button>
          <button className={`nav-item ${view === 'completed' ? 'active' : ''}`} onClick={() => changeView('completed')}>✓ Tamamlananlar</button>
          <button className={`nav-item ${view === 'reminders' ? 'active' : ''}`} onClick={() => changeView('reminders')}>◷ Hatırlatıcılar</button><button className={`nav-item ${view === 'settings' ? 'active' : ''}`} onClick={() => changeView('settings')}>⚙ Ayarlar</button>
        </nav>
        <button className="sidebar-logout" onClick={logout}>⇥ Çıkış</button>
      </aside>
      <div className="main-area">
        <header className="dashboard-header">
          <h1>{view === 'tasks' ? 'Görevlerim' : view === 'completed' ? 'Tamamlanan görevler' : view === 'reminders' ? 'Hatırlatıcılar' : 'Ayarlar'}</h1>
          <span className="profile-name">{accountEmail || 'TaskFlow kullanıcısı'}</span>
        </header>
        <main className="dashboard-content">
          {view === 'reminders' ? (<section className="settings-panel reminders-page"><div className="reminder-list"><p className="eyebrow">HATIRLATICILAR</p><h2>Planlanan bildirimler</h2><p className="reminder-intro">Yaklaşan görevlerin için oluşturduğun e-posta bildirimleri burada görünür.</p>{reminders.length === 0 ? <p className="empty-reminders">Henüz hatırlatıcı yok. Görevlerim sayfasından yeni bir bildirim oluşturabilirsin.</p> : reminders.map((reminder) => <div className="reminder-item" key={reminder.id}><strong>{reminder.taskTitle}</strong><span>{reminder.isSent ? 'Gönderildi' : `Bekliyor · ${reminder.daysBefore} gün kala gönderilecek`}</span><small>{reminder.recipientEmail}</small></div>)}</div></section>) : view === 'settings' ? (
            <section className="settings-panel">
              <p className="eyebrow">TERCİHLER</p><h2>Görünüm ayarları</h2><p>Bu seçenekler görev listesinin görünümünü düzenler.</p>
              <label>Sayfa başına görev</label>
              <select value={pageSize} onChange={(e) => { const value = Number(e.target.value); setPageSize(value); setPage(1); void saveSettings(value, sort) }}><option value="5">5 görev</option><option value="10">10 görev</option><option value="20">20 görev</option></select>
              <label>Varsayılan sıralama</label><select value={sort} onChange={(e) => { setSort(e.target.value); setPage(1); void saveSettings(pageSize, e.target.value) }}><option value="due_date">Tarih (en yakın)</option><option value="due_date_desc">Tarih (en uzak)</option><option value="title">Başlık (A-Z)</option><option value="title_desc">Başlık (Z-A)</option><option value="priority">Öncelik (yüksek)</option><option value="priority_desc">Öncelik (düşük)</option><option value="status">Durum</option></select>
            </section>
          ) : (
            <section className="task-panel">
              <div className="list-toolbar">
                <input className="header-search" type="search" placeholder="Ara..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
                <div className="toolbar-actions"><span>Sırala:</span>{sorting}<button className="new-task-button" onClick={() => { resetForm(); setIsEditorOpen(true) }}>＋ Yeni görev</button></div>
              </div>
              {taskMessage && <p className="task-message">{taskMessage}</p>}
              {tasks.length === 0 ? <div className="empty-state"><h3>Görev bulunamadı</h3><p>{view === 'completed' ? 'Tamamlanan görevler burada görünür.' : 'Yeni görev ekleyerek başlayabilirsin.'}</p></div> : (
                <div className="task-table-wrap"><table className="task-table"><thead><tr><th>Başlık</th><th>Açıklama</th><th>Öncelik</th><th>Son tarih</th><th>Kalan gün</th><th>Tamamlandı</th><th>İşlemler</th></tr></thead><tbody>
                  {tasks.map((task) => <tr key={task.id}><td className="table-title">{task.title}</td><td>{task.description}</td><td><span className={`priority priority-${task.priority}`}>{task.priority}</span></td><td>{task.dueDate ? new Date(`${dateValue(task.dueDate)}T00:00:00`).toLocaleDateString('tr-TR') : '—'}</td><td><span className={`remaining-days ${remainingClass(task.dueDate)}`}>{remainingLabel(task.dueDate)}</span></td><td><input className="complete-checkbox" type="checkbox" checked={task.status === 'Tamamlandı'} aria-label={`${task.title} tamamlandı`} onChange={(e) => updateStatus(task, e.target.checked ? 'Tamamlandı' : 'Devam Ediyor')} /></td><td><div className="task-actions"><button className="edit-button" aria-label="Görevi düzenle" onClick={() => edit(task)}>Düzenle</button><button className="delete-button" aria-label="Görevi sil" title="Sil" onClick={() => removeTask(task.id)}>🗑</button></div></td></tr>)}
                </tbody></table></div>
              )}
              <div className="pagination"><button onClick={() => setPage(page - 1)} disabled={page === 1}>‹</button><span>{page}</span><button onClick={() => setPage(page + 1)} disabled={!hasNextPage}>›</button></div>
              {view === 'tasks' && <section className="reminder-panel"><div className="reminder-heading"><p className="eyebrow">E-POSTA HATIRLATICISI</p><h2>Görev bildirimi oluştur</h2><span>Seçtiğin gün kaldığında otomatik e-posta gönderilir.</span></div><div className="reminder-fields"><label>Görev<select value={reminderTaskId} onChange={(e) => setReminderTaskId(e.target.value)}><option value="">Görev seçin</option>{tasks.filter((task) => task.dueDate).map((task) => <option value={task.id} key={task.id}>{task.title}</option>)}</select></label><label>E-posta<input type="email" value={reminderEmail} placeholder="ornek@email.com" onChange={(e) => setReminderEmail(e.target.value)} /></label><label>Kaç gün kala?<input type="number" min="0" max="365" value={reminderDays} onChange={(e) => setReminderDays(e.target.value)} /></label><button className="primary-button" onClick={createReminder}>Hatırlatıcı oluştur</button></div></section>}
            </section>
          )}
        </main>
      </div>
      {isEditorOpen && <div className="modal-backdrop" onMouseDown={resetForm}><section className="task-modal" onMouseDown={(e) => e.stopPropagation()}><div className="modal-header"><h2>{editingId === null ? 'Yeni görev' : 'Görevi düzenle'}</h2><button onClick={resetForm}>×</button></div><label>Başlık</label><input placeholder="Görev başlığı" value={title} onChange={(e) => setTitle(e.target.value)} /><label>Açıklama</label><textarea placeholder="Görev açıklaması" value={description} onChange={(e) => setDescription(e.target.value)} /><div className="form-grid"><div><label>Öncelik</label><select value={priority} onChange={(e) => setPriority(e.target.value)}><option>Düşük</option><option>Orta</option><option>Yüksek</option></select></div><div><label>Son tarih</label><input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} /></div></div><div className="form-actions"><button className="secondary-button" onClick={resetForm}>İptal</button><button className="primary-button" onClick={() => saveTask(editingId ?? undefined)}>{editingId === null ? 'Kaydet' : 'Güncelle'}</button></div></section></div>}
    </div>
  )
}
export default App
