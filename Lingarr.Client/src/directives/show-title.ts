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

        el.innerHTML = `
            <div class="${originalClasses} inline-flex items-center gap-1.5 overflow-hidden min-w-0 " >
                <span class="shrink-0">${showName}</span>
                <span>-</span>
                <span>${episodeNumber}</span>
                <span>-</span>
                <span class="text-primary-content/50  truncate block">${episodeTitle}</span>
            </div>
        `
    }
}
