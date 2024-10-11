import { reactive } from 'vue'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { EventCallbacks, Hub, SignalRState, SignalRStore } from '@/ts/composables/signalR'

let signalRInstance: SignalRStore | null = null

export const createSignalRStore = (): SignalRStore => {
    const state = reactive<SignalRState>({
        hubs: {}
    })

    const connect = async (hubName: string, url: string): Promise<Hub> => {
        if (!state.hubs[hubName]) {
            const connection = new HubConnectionBuilder()
                .withUrl(url)
                .configureLogging(LogLevel.None)
                .withAutomaticReconnect()
                .build()

            state.hubs[hubName] = {
                connection,
                isConnected: false,
                lastError: null
            }

            connection.onreconnecting(() => {
                state.hubs[hubName].isConnected = false
            })

            connection.onreconnected(() => {
                state.hubs[hubName].isConnected = true
            })

            connection.onclose(() => {
                state.hubs[hubName].isConnected = false
            })

            try {
                await connection.start()
                state.hubs[hubName].isConnected = true
            } catch (error) {
                state.hubs[hubName].lastError = error as Error
                console.error(`SignalR ${hubName} Connection Error:`, error)
            }
        }

        const hubConnection = state.hubs[hubName]

        return {
            joinGroup: async (groupName: { group: string }): Promise<void> => {
                if (hubConnection.connection) {
                    try {
                        await hubConnection.connection.invoke('JoinGroup', groupName)
                    } catch (err) {
                        console.error(
                            `Error joining group ${groupName.group} in hub ${hubName}:`,
                            err
                        )
                    }
                }
            },
            leaveGroup: async (groupName: { group: string }): Promise<void> => {
                if (hubConnection.connection) {
                    try {
                        await hubConnection.connection.invoke('LeaveGroup', groupName)
                    } catch (err) {
                        console.error(
                            `Error leaving group ${groupName.group} in hub ${hubName}:`,
                            err
                        )
                    }
                }
            },
            send: async (event: string, ...args: unknown[]): Promise<void> => {
                if (hubConnection.connection) {
                    try {
                        await hubConnection.connection.invoke(event, ...args)
                    } catch (err) {
                        console.error(`Error sending ${event} to hub ${hubName}:`, err)
                    }
                }
            },
            on: <K extends keyof EventCallbacks>(event: K, callback: EventCallbacks[K]): void => {
                if (hubConnection.connection) {
                    hubConnection.connection.on(event, callback)
                }
            },
            off: <K extends keyof EventCallbacks>(event: K, callback: EventCallbacks[K]): void => {
                if (hubConnection.connection) {
                    hubConnection.connection.off(event, callback)
                }
            }
        }
    }

    return {
        state,
        connect
    }
}

export const useSignalR = (): SignalRStore => {
    if (!signalRInstance) {
        signalRInstance = createSignalRStore()
    }
    return signalRInstance
}
