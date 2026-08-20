interface WidgetConfig {
  apiUrl: string
  wsUrl: string
}

let config: WidgetConfig = { apiUrl: '', wsUrl: '' }

export function setWidgetConfig(c: WidgetConfig) {
  config = c
}

export function getWidgetConfig() {
  return config
}
