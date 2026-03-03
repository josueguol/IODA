import type { ReactNode } from 'react'
import './ContentEditorSplitLayout.css'

interface ContentEditorSplitLayoutProps {
  left: ReactNode
  right: ReactNode
}

export function ContentEditorSplitLayout({ left, right }: ContentEditorSplitLayoutProps) {
  return (
    <div className="content-editor-split">
      <section className="content-editor-split__left">{left}</section>
      <aside className="content-editor-split__right">{right}</aside>
    </div>
  )
}
