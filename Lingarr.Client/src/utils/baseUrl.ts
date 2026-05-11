const baseHref = document.querySelector('base')?.getAttribute('href') ?? '/'

export const baseUrl = (): string => baseHref

export const resolveUrl = (path: string): string =>
    new URL(path.replace(/^\//, ''), new URL(baseHref, location.origin)).toString()
