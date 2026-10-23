export default {
    mounted(el: HTMLElement) {
        const text = el.textContent
        if (!text) return

        const parts = text.split(/`([^`]+)`/g)
        const nodes = parts.map((part, index) => {
            if (index % 2 === 0) {
                return document.createTextNode(part)
            }
            const code = document.createElement('code')
            code.className = 'px-1 py-0.5 rounded-sm bg-primary text-primary-content'
            code.textContent = part
            return code
        })

        el.replaceChildren(...nodes)
    }
}
