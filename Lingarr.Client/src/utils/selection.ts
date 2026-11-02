export type SelectionMode = 'ids' | 'query'

export interface SelectionState<T extends { id: number }> {
    mode: SelectionMode
    selected: T[]
    excludeIds: number[]
}

export function emptySelection<T extends { id: number }>(): SelectionState<T> {
    return { mode: 'ids', selected: [], excludeIds: [] }
}

export function isItemSelected<T extends { id: number }>(
    state: SelectionState<T>,
    id: number
): boolean {
    if (state.mode === 'query') {
        return !state.excludeIds.includes(id)
    }
    return state.selected.some((item) => item.id === id)
}

export function isPageSelected<T extends { id: number }>(
    state: SelectionState<T>,
    items: T[]
): boolean {
    return items.length > 0 && items.every((item) => isItemSelected(state, item.id))
}

export function selectionCount<T extends { id: number }>(
    state: SelectionState<T>,
    totalCount: number
): number {
    if (state.mode === 'query') {
        return Math.max(0, totalCount - state.excludeIds.length)
    }
    return state.selected.length
}

export function toggleItem<T extends { id: number }>(
    state: SelectionState<T>,
    item: T
): SelectionState<T> {
    if (state.mode === 'query') {
        const excludeIds = state.excludeIds.includes(item.id)
            ? state.excludeIds.filter((id) => id !== item.id)
            : [...state.excludeIds, item.id]
        return { ...state, excludeIds }
    }

    const selected = state.selected.some((selectedItem) => selectedItem.id === item.id)
        ? state.selected.filter((selectedItem) => selectedItem.id !== item.id)
        : [...state.selected, item]
    return { ...state, selected }
}

export function togglePage<T extends { id: number }>(
    state: SelectionState<T>,
    items: T[]
): SelectionState<T> {
    if (isPageSelected(state, items)) {
        if (state.mode === 'query') {
            return emptySelection()
        }
        const pageIds = new Set(items.map((item) => item.id))
        return {
            ...state,
            selected: state.selected.filter((item) => !pageIds.has(item.id))
        }
    }

    if (state.mode === 'query') {
        const pageIds = new Set(items.map((item) => item.id))
        return {
            ...state,
            excludeIds: state.excludeIds.filter((id) => !pageIds.has(id))
        }
    }

    const byId = new Map(state.selected.map((item) => [item.id, item]))
    for (const item of items) {
        byId.set(item.id, item)
    }
    return { ...state, selected: [...byId.values()] }
}

export function selectAllMatching<T extends { id: number }>(): SelectionState<T> {
    return { mode: 'query', selected: [], excludeIds: [] }
}
