export default {
    mounted(el: HTMLElement) {
        const text = el.textContent
        if (!text) return

        el.innerHTML = text.replace(
            /`([^`]+)`/g,
            '<code class="px-1 py-0.5 rounded-sm bg-primary text-primary-content">$1</code>'
        )
    }
}
