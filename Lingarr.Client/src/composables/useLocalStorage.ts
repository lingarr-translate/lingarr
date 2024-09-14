import { ref, Ref } from 'vue'

interface UseLocalStorage {
    storagePrefix: Ref<string>
    getKey: (key: string) => string
    getItem: <T>(key: string) => T | null
    setItem: <T>(key: string, value: T) => void
}

export function useLocalStorage(defaultPrefix = 'lingarr_'): UseLocalStorage {
    const storagePrefix = ref(defaultPrefix)

    function getKey(key: string): string {
        return storagePrefix.value + key
    }

    function getItem<T>(key: string): T | null {
        const prefixedKey = getKey(key)
        const value = localStorage.getItem(prefixedKey)
        return value ? JSON.parse(value) : null
    }

    function setItem<T>(key: string, value: T): void {
        const prefixedKey = getKey(key)
        localStorage.setItem(prefixedKey, JSON.stringify(value))
    }

    return {
        storagePrefix,
        getKey,
        getItem,
        setItem
    }
}
