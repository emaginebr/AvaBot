const COOKIE_EXPIRY_DAYS = 30

const getCookieName = (slug: string, field: string): string =>
  `avabot_${slug}_${field}`

const setCookie = (name: string, value: string, days: number): void => {
  const expires = new Date(Date.now() + days * 864e5).toUTCString()
  document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/; SameSite=Lax`
}

const getCookie = (name: string): string | null => {
  const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`))
  return match ? decodeURIComponent(match[1]) : null
}

const deleteCookie = (name: string): void => {
  document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/; SameSite=Lax`
}

interface SessionCookieData {
  resumeToken: string
  userName: string
  userEmail?: string
  userPhone?: string
}

const FIELDS = ['resumeToken', 'userName', 'userEmail', 'userPhone'] as const

export const CookieService = {
  setCookies: (slug: string, data: SessionCookieData): void => {
    for (const field of FIELDS) {
      const value = data[field as keyof SessionCookieData]
      if (value) {
        setCookie(getCookieName(slug, field), value, COOKIE_EXPIRY_DAYS)
      }
    }
  },

  getCookies: (slug: string): SessionCookieData | null => {
    const resumeToken = getCookie(getCookieName(slug, 'resumeToken'))
    const userName = getCookie(getCookieName(slug, 'userName'))
    if (!resumeToken || !userName) return null

    return {
      resumeToken,
      userName,
      userEmail: getCookie(getCookieName(slug, 'userEmail')) ?? undefined,
      userPhone: getCookie(getCookieName(slug, 'userPhone')) ?? undefined,
    }
  },

  clearCookies: (slug: string): void => {
    for (const field of FIELDS) {
      deleteCookie(getCookieName(slug, field))
    }
  },
}
