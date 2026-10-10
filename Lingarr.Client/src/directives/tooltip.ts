import type { ObjectDirective } from 'vue'

type TooltipState = { update: (text: string) => void; destroy: () => void }
const tooltips = new WeakMap<HTMLElement, TooltipState>()
let nextId = 0

/** Hover and keyboard hints, rendered outside scroll containers to avoid clipping. */
const tooltip: ObjectDirective<HTMLElement, string> = {
    mounted(el, binding) {
        let text = binding.value
        let bubble: HTMLDivElement | undefined
        let timer: ReturnType<typeof setTimeout> | undefined
        const id = `lingarr-tooltip-${++nextId}`
        const controller = new AbortController()
        const options = { signal: controller.signal }

        const hide = () => {
            clearTimeout(timer)
            bubble?.remove()
            bubble = undefined
            const descriptions = (el.getAttribute('aria-describedby') ?? '')
                .split(' ')
                .filter((value) => value && value !== id)
            if (descriptions.length) el.setAttribute('aria-describedby', descriptions.join(' '))
            else el.removeAttribute('aria-describedby')
        }
        const delayHide = () => {
            clearTimeout(timer)
            timer = setTimeout(hide, 120)
        }
        const show = () => {
            clearTimeout(timer)
            if (!text || bubble) return
            bubble = document.createElement('div')
            bubble.id = id
            bubble.role = 'tooltip'
            bubble.className = 'ui-tooltip'
            bubble.textContent = text
            document.body.appendChild(bubble)
            const anchor = el.getBoundingClientRect()
            const rect = bubble.getBoundingClientRect()
            bubble.style.left = `${Math.max(8, Math.min(anchor.left + anchor.width / 2 - rect.width / 2, window.innerWidth - rect.width - 8))}px`
            bubble.style.top = `${anchor.top >= rect.height + 12 ? anchor.top - rect.height - 8 : Math.min(anchor.bottom + 8, window.innerHeight - rect.height - 8)}px`
            el.setAttribute(
                'aria-describedby',
                [el.getAttribute('aria-describedby'), id].filter(Boolean).join(' ')
            )
            bubble.addEventListener('pointerenter', () => clearTimeout(timer))
            bubble.addEventListener('pointerleave', delayHide)
        }
        el.addEventListener('pointerenter', show, options)
        el.addEventListener('pointerleave', delayHide, options)
        el.addEventListener('focusin', show, options)
        el.addEventListener('focusout', hide, options)
        el.addEventListener('pointerdown', hide, options)
        document.addEventListener(
            'keydown',
            (event) => {
                if (event.key === 'Escape') hide()
            },
            options
        )
        window.addEventListener('scroll', hide, { ...options, capture: true })
        window.addEventListener('resize', hide, options)
        tooltips.set(el, {
            update(value) {
                text = value
                if (bubble) bubble.textContent = value
            },
            destroy() {
                hide()
                controller.abort()
            }
        })
    },
    updated(el, binding) {
        tooltips.get(el)?.update(binding.value)
    },
    unmounted(el) {
        tooltips.get(el)?.destroy()
        tooltips.delete(el)
    }
}

export default tooltip
