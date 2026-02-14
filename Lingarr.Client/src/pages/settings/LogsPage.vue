<template>
    <div class="w-full bg-secondary p-4">
        <div class="mb-4 border-b-2 border-secondary bg-primary font-bold text-secondary-content">
            <div class="flex items-center justify-between px-4 py-3">
                <h1 class="text-xl">System Logs</h1>
                <div class="flex items-center space-x-3">
                    <!-- Filters -->
                    <div class="flex items-center space-x-4">
                        <select
                            v-model="filterOptions.logLevel"
                            class="rounded border border-secondary bg-secondary px-2 py-1 text-sm text-accent-content">
                            <option value="all">All Levels</option>
                            <option value="information">Information</option>
                            <option value="warning">Warning</option>
                            <option value="error">Error</option>
                        </select>
                    </div>

                    <div class="flex space-x-2">
                        <button
                            class="hover:bg-accent/80 cursor-pointer rounded bg-accent px-3 py-1 text-sm font-medium text-white transition"
                            @click="exportLogs">
                            Export
                        </button>
                        <button
                            class="bg-warning hover:bg-warning/80 cursor-pointer rounded px-3 py-1 text-sm font-medium text-white transition"
                            @click="toggleAutoScroll">
                            {{ autoScroll ? 'Disable Auto-scroll' : 'Enable Auto-scroll' }}
                        </button>
                        <button
                            class="bg-error hover:bg-error/80 cursor-pointer rounded px-3 py-1 text-sm font-medium text-white transition"
                            @click="clearLogs">
                            Clear
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <div
            class="grid grid-cols-12 border-b-2 border-secondary bg-primary font-bold text-secondary-content">
            <div class="col-span-1 px-4 py-2">Time</div>
            <div class="col-span-1 px-4 py-2">Level</div>
            <div class="col-span-3 px-4 py-2">Source</div>
            <div class="col-span-5 px-4 py-2 md:col-span-7">Message</div>
        </div>

        <div
            ref="logContainer"
            class="h-[70vh] overflow-y-auto bg-primary font-mono text-sm text-accent-content">
            <div v-if="filteredLogs.length === 0" class="flex h-full items-center justify-center">
                <div class="text-center text-gray-500">
                    <div class="mb-2 text-lg">📋</div>
                    <div>Waiting for logs...</div>
                </div>
            </div>

            <!-- Log Entries -->
            <div v-for="(log, index) in filteredLogs" :key="index" class="log-entry">
                <div
                    class="hover:bg-secondary/20 border-secondary/30 grid grid-cols-12 border-b py-2 transition-colors">
                    <div class="col-span-1 px-4 text-gray-400">
                        {{ log.formattedTime }}
                    </div>
                    <div class="col-span-1 px-4">
                        <span
                            :class="getLogLevelBadgeClass(log.logLevel)"
                            class="rounded px-2 py-1 text-xs font-medium">
                            {{ log.logLevel.toUpperCase() }}
                        </span>
                    </div>
                    <div class="col-span-3 px-4 text-blue-300">
                        {{ log.formattedSource }}
                    </div>
                    <div
                        class="col-span-5 px-4 md:col-span-7"
                        v-html="formatLogMessage(log.message)"></div>
                </div>

                <div
                    v-if="log.stackTrace"
                    class="border-secondary/30 bg-error/5 ml-6 border-b py-2 pl-12 pr-4 text-xs">
                    <pre class="whitespace-pre-wrap">{{ log.stackTrace }}</pre>
                </div>
            </div>
        </div>

        <!-- Footer Stats -->
        <div
            class="mt-4 flex justify-between border-t-2 border-secondary bg-primary px-4 py-2 text-sm text-secondary-content">
            <div>Total entries: {{ filteredLogs.length }}</div>
            <div>
                Auto-scroll:
                <span :class="autoScroll ? 'text-success' : 'text-error'">
                    {{ autoScroll ? 'Enabled' : 'Disabled' }}
                </span>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, computed, watch } from 'vue'
import { ILogEntry, IFilterOptions } from '@/ts'
import services from '@/services'

const logs = ref<ILogEntry[]>([])
const autoScroll = ref(true)
const logContainer = ref<HTMLElement | null>(null)
const filterOptions = ref<IFilterOptions>({
    logLevel: 'all'
})
let eventSource: EventSource | null = null

const filteredLogs = computed(() => {
    return logs.value.filter((log) => {
        if (filterOptions.value.logLevel !== 'all') {
            const logLevel = log.logLevel.toLowerCase()
            if (logLevel !== filterOptions.value.logLevel.toLowerCase()) {
                return false
            }
        }
        return true
    })
})

const formatLogMessage = (message: string): string => {
    // Replace color tags
    let formattedMessage = message
        .replace(/\|Green\|([^|]+)\|\/Green\|/g, '<span class="text-green-500">$1</span>')
        .replace(/\|Red\|([^|]+)\|\/Red\|/g, '<span class="text-red-500">$1</span>')
        .replace(/\|Orange\|([^|]+)\|\/Orange\|/g, '<span class="text-orange-500">$1</span>')

    // Highlight environment variables
    formattedMessage = formattedMessage.replace(
        /'([A-Z_]+)'/g,
        '<span class="text-accent">\'$1\'</span>'
    )

    return formattedMessage
}

const getLogLevelBadgeClass = (level: string): string => {
    const levelLower = level.toLowerCase()
    if (levelLower.includes('error')) return 'bg-red-500/20 text-red-500'
    if (levelLower.includes('warning')) return 'bg-orange-500/20 text-orange-500'
    if (levelLower.includes('information')) return 'bg-green-500/20 text-green-500'
    return 'bg-info/20 text-info'
}

const scrollToBottom = async () => {
    if (autoScroll.value && logContainer.value) {
        await nextTick()
        logContainer.value.scrollTop = logContainer.value.scrollHeight
    }
}

const toggleAutoScroll = () => {
    autoScroll.value = !autoScroll.value
    if (autoScroll.value) {
        scrollToBottom()
    }
}

const clearLogs = () => {
    logs.value = []
}

const exportLogs = () => {
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-')
    const filename = `system-logs-${timestamp}.txt`

    let exportContent = `System Logs Export\n`
    exportContent += `Generated: ${new Date().toLocaleString()}\n`
    exportContent += `Total Entries: ${filteredLogs.value.length}\n`
    exportContent += `${'='.repeat(80)}\n\n`

    filteredLogs.value.forEach((log) => {
        exportContent += `[${log.formattedDate} ${log.formattedTime}] [${log.logLevel}] [${log.category}] ${log.message}\n`

        // Include stack trace
        if (log.stackTrace) {
            exportContent += `Stack Trace:\n${log.stackTrace}\n`
        }

        exportContent += `\n`
    })

    const blob = new Blob([exportContent], { type: 'text/plain' })
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = filename
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
}

watch(
    filterOptions,
    () => {
        scrollToBottom()
    },
    { deep: true }
)

onMounted(() => {
    eventSource = services.logs.getStream()

    eventSource.onmessage = (event) => {
        try {
            const logData = JSON.parse(event.data)
            logs.value = [...logs.value, logData]
            scrollToBottom()
        } catch (error) {
            console.error('Error processing log entry:', error)
            console.error('Problematic data:', event.data)

            const fallbackEntry: ILogEntry = {
                logLevel: 'Error',
                message: `Failed to process log data: ${typeof event.data === 'string' ? event.data.substring(0, 100) + '...' : 'Invalid format'}`,
                formattedTime: new Date().toTimeString().split(' ')[0],
                formattedDate: new Date().toDateString(),
                formattedSource: 'System',
                category: 'System',
                stackTrace: error instanceof Error ? error.stack : undefined
            }

            logs.value = [...logs.value, fallbackEntry]
        }
    }

    eventSource.onerror = (error) => {
        console.error('EventSource error:', error)
        logs.value.push({
            logLevel: 'error',
            message: `Log stream connection error. Attempting to reconnect in 5 seconds...`,
            formattedTime: new Date().toTimeString().split(' ')[0],
            formattedDate: new Date().toLocaleDateString(),
            formattedSource: 'System',
            category: 'System'
        })
        // reconnect
        if (eventSource) {
            eventSource.close()
            setTimeout(() => {
                eventSource = services.logs.getStream()
            }, 5000)
        }
    }
})

onUnmounted(() => {
    if (eventSource) {
        eventSource.close()
        eventSource = null
    }
})
</script>
