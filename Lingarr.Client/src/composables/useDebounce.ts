export const useDebounce = <T extends (...args: never[]) => void>(
    fn: T,
    delay: number
): ((...args: Parameters<T>) => void) => {
    let timeout: NodeJS.Timeout | undefined

    return (...args: Parameters<T>) => {
        if (timeout) {
            clearTimeout(timeout)
        }
        timeout = setTimeout(() => {
            fn(...args)
        }, delay)
    }
}

export default useDebounce
