import * as signalR from '@microsoft/signalr'
import { reactive } from 'vue'
import type { SignalRState, SignalRStore } from '@/ts'

let signalRInstance: SignalRStore | null = null

export const createSignalRStore = (): SignalRStore => {
    const state = reactive<SignalRState>({
        connection: null,
        isConnected: false,
        lastError: null
    })

    const start = async (url: string): Promise<void> => {
        state.connection = new signalR.HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build()

        state.connection.onreconnecting((error: any) => {
            state.isConnected = false
            console.warn('SignalR reconnecting...', error)
        })

        state.connection.onreconnected((connectionId: any) => {
            state.isConnected = true
            console.log('SignalR reconnected', connectionId)
        })

        state.connection.onclose(async () => {
            state.isConnected = false
            console.error('SignalR closed, restarting connection...')
            await start(url)
        })

        try {
            await state.connection.start()
            state.isConnected = true
            console.log('SignalR Connected')
        } catch (error) {
            state.lastError = error as Error
            console.error('SignalR Connection Error:', error)
        }
    }

    const joinGroup = async (groupName: { group: string }): Promise<void> => {
        if (state.connection) {
            try {
                await state.connection.invoke('JoinGroup', groupName)
                console.log(`Joined group: ${groupName.group}`)
            } catch (err) {
                console.error(`Error joining group ${groupName.group}:`, err)
            }
        }
    }

    const leaveGroup = async (groupName: { group: string }): Promise<void> => {
        if (state.connection) {
            try {
                await state.connection.invoke('LeaveGroup', groupName)
                console.log(`Left group: ${groupName.group}`)
            } catch (err) {
                console.error(`Error leaving group ${groupName.group}:`, err)
            }
        }
    }

    const send = async (event: string, ...args: any[]): Promise<void> => {
        if (state.connection) {
            try {
                await state.connection.invoke(event, ...args)
            } catch (err) {
                console.error(`Error sending ${event}:`, err)
            }
        }
    }

    const on = (event: string, callback: (...args: any[]) => void): void => {
        if (state.connection) {
            state.connection.on(event, callback)
        }
    }

    const off = (event: string, callback: (...args: any[]) => void): void => {
        if (state.connection) {
            state.connection.off(event, callback)
        }
    }

    return {
        state,
        start,
        joinGroup,
        leaveGroup,
        send,
        on,
        off
    }
}

export const useSignalR = (): SignalRStore => {
    if (!signalRInstance) {
        signalRInstance = createSignalRStore()
    }
    return signalRInstance
}

export const createSignalRPlugin = (options: { url: string }) => ({
    install: () => {
        const signalRStore = useSignalR()
        signalRStore.start(options.url)
    }
})
