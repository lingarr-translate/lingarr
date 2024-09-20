export interface SignalRState {
    connection: any | null
    isConnected: boolean
    lastError: Error | null
}

export interface SignalRStore {
    state: SignalRState
    start: (url: string) => Promise<void>
    joinGroup: (groupName: { group: string }) => Promise<void>
    leaveGroup: (groupName: { group: string }) => Promise<void>
    send: (event: string, ...args: any[]) => Promise<void>
    on: (event: string, callback: (...args: any[]) => void) => void
    off: (event: string, callback: (...args: any[]) => void) => void
}
