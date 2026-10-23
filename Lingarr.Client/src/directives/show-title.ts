export default {
    mounted(el: HTMLElement) {
        const text = el.textContent
        const originalClasses = el.className
        if (!text) return

        const parts = text.split(' - ')

        if (parts.length !== 3) {
            return
        }
        const [showName, episodeNumber, episodeTitle] = parts

        const container = document.createElement('div')
        container.className = `${originalClasses} inline-flex items-center gap-1.5 overflow-hidden min-w-0 w-full`
        container.append(
            createSpan(showName, 'shrink-0'),
            createSpan('-'),
            createSpan(episodeNumber),
            createSpan('-'),
            createSpan(episodeTitle, 'text-primary-content/50 truncate block')
        )

        el.replaceChildren(container)
    }
}

function createSpan(text: string, className?: string) {
    const span = document.createElement('span')
    span.textContent = text
    if (className) {
        span.className = className
    }
    return span
}
