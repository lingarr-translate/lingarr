import { IRequestProgress, ISettings } from '@/ts'

export interface SignalRStore {
    state: SignalRState
    connect: (hubName: string, url: string) => Promise<Hub>
}

export interface SignalRState {
    hubs: Record<string, HubConnection>
}

interface HubConnection {
    connection: signalR.HubConnection
    isConnected: boolean
    lastError: Error | null
}

export type EventCallbacks = {
    GroupCompleted: (group: string) => void
    SettingUpdate: (setting: { key: keyof ISettings; value: string }) => void
    RequestProgress: (requestProgress: IRequestProgress) => void
    RequestActive: (request: { count: number }) => void
    // Completed: (translationRequest: ITranslationRequest) => void
}

export interface Hub {
    joinGroup: (groupName: { group: string }) => Promise<void>
    leaveGroup: (groupName: { group: string }) => Promise<void>
    send: (event: string, ...args: unknown[]) => Promise<void>
    on<K extends keyof EventCallbacks>(event: K, callback: EventCallbacks[K]): void
    off<K extends keyof EventCallbacks>(event: K, callback: EventCallbacks[K]): void
}
