import React from 'react'
import ReactDOM from 'react-dom/client'
import { OverlayWindow } from './features/overlay/OverlayWindow'

ReactDOM.createRoot(document.getElementById('overlay-root')!).render(
  <React.StrictMode>
    <OverlayWindow />
  </React.StrictMode>,
)
