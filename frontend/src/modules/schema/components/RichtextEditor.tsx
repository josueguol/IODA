import { useCallback, useEffect, useMemo } from 'react'
import { BlockNoteSchema, type PartialBlock } from '@blocknote/core'
import { en } from '@blocknote/core/locales'
import { filterSuggestionItems } from '@blocknote/core/extensions'
import {
  BasicTextStyleButton,
  BlockTypeSelect,
  ColorStyleButton,
  CreateLinkButton,
  FormattingToolbar,
  FormattingToolbarController,
  getDefaultReactSlashMenuItems,
  type DefaultReactSuggestionItem,
  NestBlockButton,
  SuggestionMenuController,
  TextAlignButton,
  UnnestBlockButton,
  useCreateBlockNote,
  blockTypeSelectItems,
} from '@blocknote/react'
import { BlockNoteView } from '@blocknote/mantine'
import { getMultiColumnSlashMenuItems, locales as multiColumnLocales, multiColumnDropCursor, withMultiColumn } from '@blocknote/xl-multi-column'
import '@blocknote/core/fonts/inter.css'
import '@blocknote/mantine/style.css'
import './RichtextEditor.css'

interface BlockNoteFieldPayload {
  format: 'blocknote_markdown_v1'
  markdown: string
  blocks: unknown[]
  metadata: {
    hasTwoColumns: boolean
    hasThreeColumns: boolean
    embeds: Array<{ url: string }>
    modules: Array<{ key: string }>
  }
}

function toPayload(raw: unknown): BlockNoteFieldPayload | null {
  if (typeof raw !== 'string' || raw.trim().length === 0) return null
  try {
    const parsed = JSON.parse(raw) as Partial<BlockNoteFieldPayload>
    if (!parsed || typeof parsed !== 'object') return null
    if (typeof parsed.markdown !== 'string') return null
    return {
      format: 'blocknote_markdown_v1',
      markdown: parsed.markdown,
      blocks: Array.isArray(parsed.blocks) ? parsed.blocks : [],
      metadata: {
        hasTwoColumns: Boolean(parsed.metadata?.hasTwoColumns),
        hasThreeColumns: Boolean(parsed.metadata?.hasThreeColumns),
        embeds: Array.isArray(parsed.metadata?.embeds) ? parsed.metadata?.embeds.filter((x): x is { url: string } => !!x && typeof x.url === 'string') : [],
        modules: Array.isArray(parsed.metadata?.modules) ? parsed.metadata?.modules.filter((x): x is { key: string } => !!x && typeof x.key === 'string') : [],
      },
    }
  } catch {
    return null
  }
}

export function RichtextEditor({
  value,
  onChange,
}: {
  value: unknown
  onChange: (nextValue: string) => void
}) {
  const parsedValue = useMemo(() => toPayload(value), [value])
  const initialContent = useMemo<PartialBlock[] | undefined>(
    () => (Array.isArray(parsedValue?.blocks) ? (parsedValue.blocks as unknown as PartialBlock[]) : undefined),
    [parsedValue]
  )
  const initialMarkdown = parsedValue?.markdown ?? (typeof value === 'string' ? value : '')
  const schema = useMemo(() => withMultiColumn(BlockNoteSchema.create()), [])

  const editor = useCreateBlockNote({
    schema,
    dropCursor: multiColumnDropCursor,
    dictionary: {
      ...en,
      multi_column: multiColumnLocales.en,
    },
    initialContent,
  })

  useEffect(() => {
    if (initialContent && initialContent.length > 0) return
    if (!initialMarkdown || initialMarkdown.trim().length === 0) return

    const markdownBlocks = editor.tryParseMarkdownToBlocks(initialMarkdown)
    if (markdownBlocks.length > 0) {
      editor.replaceBlocks(editor.document, markdownBlocks)
    }
  }, [editor, initialContent, initialMarkdown])

  useEffect(() => {
    return editor.onChange(() => {
      const markdown = editor.blocksToMarkdownLossy(editor.document)
      const docJson = editor.document as unknown[]
      const payload: BlockNoteFieldPayload = {
        format: 'blocknote_markdown_v1',
        markdown,
        blocks: docJson,
        metadata: {
          hasTwoColumns: hasColumns(docJson, 2),
          hasThreeColumns: hasColumns(docJson, 3),
          embeds: extractEmbeds(markdown),
          modules: extractModules(markdown),
        },
      }
      onChange(JSON.stringify(payload))
    })
  }, [editor, onChange])

  const insertEmbedTemplate = useCallback(() => {
    const current = editor.getTextCursorPosition().block
    const blocks = [{ type: 'paragraph', content: '[Embed](https://www.youtube.com/watch?v=VIDEO_ID)' }] as unknown as PartialBlock[]
    editor.insertBlocks(blocks, current, 'after')
  }, [editor])

  const insertComponentModuleTemplate = useCallback(() => {
    const current = editor.getTextCursorPosition().block
    const blocks = [{ type: 'paragraph', content: '{{component:module-key}}' }] as unknown as PartialBlock[]
    editor.insertBlocks(blocks, current, 'after')
  }, [editor])

  const toolbarBlockTypes = useMemo(() => {
    return blockTypeSelectItems(editor.dictionary).filter((item) => {
      if (item.type === 'heading') {
        const level = Number(item.props?.level ?? 0)
        return [2, 3, 4, 5, 6].includes(level)
      }
      return [
        'paragraph',
        'quote',
        'bulletListItem',
        'numberedListItem',
        'codeBlock',
      ].includes(item.type)
    })
  }, [editor.dictionary])

  const getSlashItems = useCallback(async (query: string): Promise<DefaultReactSuggestionItem[]> => {
    const defaults = getDefaultReactSlashMenuItems(editor).filter((item) => {
      const itemWithKey = item as unknown as { key?: string }
      const key = (itemWithKey.key ?? '').toString()
      return (
        key !== 'heading' &&
        key !== 'toggle_heading' &&
        key !== 'toggle_heading_2' &&
        key !== 'toggle_heading_3' &&
        key !== 'check_list' &&
        key !== 'toggle_list'
      )
    })
    const multiColumnItems = (getMultiColumnSlashMenuItems(editor) as unknown as DefaultReactSuggestionItem[]).map((item) => ({
      ...item,
      group: 'Layouts',
    }))
    const customItems: DefaultReactSuggestionItem[] = [
      {
        title: 'Embed',
        subtext: 'Inserta template de contenido embebido',
        aliases: ['youtube', 'vimeo', 'iframe', 'social'],
        group: 'Custom Media',
        onItemClick: () => insertEmbedTemplate(),
      },
      {
        title: 'Component module',
        subtext: 'Inserta placeholder de módulo de componente',
        aliases: ['module', 'component', 'widget'],
        group: 'Custom Integrations',
        onItemClick: () => insertComponentModuleTemplate(),
      },
    ]
    const filtered = filterSuggestionItems([...multiColumnItems, ...defaults, ...customItems], query)
    return ensureUniqueSuggestionTitles(filtered)
  }, [editor, insertComponentModuleTemplate, insertEmbedTemplate])

  return (
    <div className="blocknote-field">
      <div className="blocknote-field__editor">
        <BlockNoteView
          editor={editor}
          formattingToolbar={false}
          slashMenu={false}
          sideMenu
          filePanel
          tableHandles
          linkToolbar
          emojiPicker
        >
          <FormattingToolbarController
            formattingToolbar={() => (
              <FormattingToolbar>
                <BlockTypeSelect items={toolbarBlockTypes} />
                <BasicTextStyleButton basicTextStyle="bold" />
                <BasicTextStyleButton basicTextStyle="italic" />
                <BasicTextStyleButton basicTextStyle="underline" />
                <BasicTextStyleButton basicTextStyle="strike" />
                <TextAlignButton textAlignment="left" />
                <TextAlignButton textAlignment="center" />
                <TextAlignButton textAlignment="right" />
                <CreateLinkButton />
                <ColorStyleButton />
                <NestBlockButton />
                <UnnestBlockButton />
              </FormattingToolbar>
            )}
          />
          <SuggestionMenuController triggerCharacter="/" getItems={getSlashItems} />
        </BlockNoteView>
      </div>
    </div>
  )
}

function ensureUniqueSuggestionTitles(items: DefaultReactSuggestionItem[]): DefaultReactSuggestionItem[] {
  const seen = new Map<string, number>()
  return items.map((item) => {
    const baseTitle = item.title?.trim() || 'Item'
    const count = (seen.get(baseTitle) ?? 0) + 1
    seen.set(baseTitle, count)
    if (count === 1) return item
    return { ...item, title: `${baseTitle} (${count})` }
  })
}

function hasColumns(blocks: unknown[], columns: number): boolean {
  if (!Array.isArray(blocks)) return false
  return blocks.some((block) => {
    const typed = block as { type?: string; children?: unknown[] }
    return typed.type === 'columnList' && Array.isArray(typed.children) && typed.children.length === columns
  })
}

function extractEmbeds(markdown: string): Array<{ url: string }> {
  const matches = markdown.match(/\]\((https?:\/\/[^\s)]+)\)/gi) ?? []
  return matches
    .map((entry) => {
      const start = entry.indexOf('(')
      const end = entry.lastIndexOf(')')
      const url = start >= 0 && end > start ? entry.slice(start + 1, end) : ''
      return { url }
    })
    .filter((x) => x.url.length > 0)
}

function extractModules(markdown: string): Array<{ key: string }> {
  const matches = markdown.match(/\{\{component:([a-zA-Z0-9._-]+)\}\}/g) ?? []
  return matches
    .map((entry) => {
      const key = entry.replace('{{component:', '').replace('}}', '').trim()
      return { key }
    })
    .filter((x) => x.key.length > 0)
}
