<template>
    <section class="logs-page w-full p-4" aria-label="System logs">
        <div class="logs-panel">
            <div class="logs-toolbar">
                <h2 class="text-xl font-semibold">System Logs</h2>
                <div class="logs-actions">
                    <select
                        v-model="filterOptions.logLevel"
                        aria-label="Filter log level"
                        class="ui-input px-3 py-2 text-sm">
                        <option value="all">All Levels</option>
                        <option value="information">Information</option>
                        <option value="warning">Warning</option>
                        <option value="error">Error</option>
                    </select>
                    <ButtonComponent size="sm" variant="primary" @click="exportLogs">
                        Export
                    </ButtonComponent>
                    <ButtonComponent
                        size="sm"
                        variant="secondary"
                        :aria-pressed="autoScroll"
                        v-tooltip="'Automatically follow new log entries.'"
                        @click="toggleAutoScroll">
                        Auto-scroll: {{ autoScroll ? 'On' : 'Off' }}
                    </ButtonComponent>
                    <ButtonComponent size="sm" variant="ghost" @click="clearLogs">
                        Clear
                    </ButtonComponent>
                </div>
            </div>

            <div ref="logContainer" class="logs-scroll" tabindex="0" aria-label="Log entries">
                <div class="log-grid log-heading" aria-hidden="true">
                    <div>Time</div>
                    <div>Level</div>
                    <div>Source</div>
                    <div>Message</div>
                </div>
                <div v-if="filteredLogs.length === 0" class="logs-empty text-secondary-content">
                    <div class="mb-2 text-lg" aria-hidden="true">📋</div>
                    <div>Waiting for logs...</div>
                </div>
                <article v-for="(log, index) in filteredLogs" :key="index" class="log-entry">
                    <div class="log-grid">
                        <time class="log-time text-secondary-content" :datetime="log.timestamp">
                            {{ formatTime(log.timestamp) }}
                        </time>
                        <div class="log-level-cell">
                            <span :class="getLogLevelBadgeClass(log.logLevel)" class="log-level">
                                {{ log.logLevel.toUpperCase() }}
                            </span>
                        </div>
                        <div class="log-source">{{ log.formattedSource }}</div>
                        <div class="log-message" v-html="formatLogMessage(log.message)"></div>
                    </div>
                    <pre v-if="log.stackTrace" class="log-stack">{{ log.stackTrace }}</pre>
                </article>
            </div>

            <div class="logs-footer text-secondary-content text-sm">
                <span>Total entries: {{ filteredLogs.length }}</span>
                <span>Auto-scroll: {{ autoScroll ? 'Enabled' : 'Disabled' }}</span>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick, computed, watch } from 'vue'
import { ILogEntry, IFilterOptions } from '@/ts'
import services from '@/services'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import { formatDate, formatTime } from '@/utils/date'

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
    // Keep log text literal before adding the supported color markers.
    let formattedMessage = message
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/\|Green\|([^|]+)\|\/Green\|/g, '<span class="log-highlight-green">$1</span>')
        .replace(/\|Red\|([^|]+)\|\/Red\|/g, '<span class="log-highlight-red">$1</span>')
        .replace(/\|Orange\|([^|]+)\|\/Orange\|/g, '<span class="log-highlight-orange">$1</span>')

    // Highlight environment variables
    formattedMessage = formattedMessage.replace(
        /'([A-Z_]+)'/g,
        '<span class="text-accent">\'$1\'</span>'
    )

    return formattedMessage
}

const getLogLevelBadgeClass = (level: string): string => {
    const levelLower = level.toLowerCase()
    if (levelLower.includes('error')) return 'log-level-error'
    if (levelLower.includes('warning')) return 'log-level-warning'
    if (levelLower.includes('information')) return 'log-level-information'
    return 'log-level-other'
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
        exportContent += `[${formatDate(log.timestamp)} ${formatTime(log.timestamp)}] [${log.logLevel}] [${log.category}] ${log.message}\n`

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
                timestamp: new Date().toISOString(),
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
            timestamp: new Date().toISOString(),
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

<style scoped>
.logs-panel {
    container: logs / inline-size;
    min-width: 0;
    border: 1px solid color-mix(in srgb, var(--primary-content) 20%, transparent);
    border-radius: 0.75rem;
    background: var(--primary);
}
.logs-toolbar,
.logs-actions,
.logs-footer {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.75rem;
}
.logs-toolbar,
.logs-footer {
    justify-content: space-between;
    padding: 0.75rem 1rem;
    background: var(--tertiary);
}
.logs-toolbar {
    border-radius: 0.75rem 0.75rem 0 0;
}
.logs-actions {
    gap: 0.5rem;
}
.logs-actions button {
    white-space: nowrap;
}
.logs-scroll {
    height: min(65dvh, 48rem);
    min-height: 16rem;
    overflow: auto;
    scrollbar-gutter: stable;
}
.log-grid {
    display: grid;
    grid-template-columns: auto minmax(0, 1fr);
    grid-template-areas: 'time level' 'source source' 'message message';
    align-items: start;
    gap: 0.375rem 1rem;
    padding: 0.625rem 1rem;
}
.log-grid > * {
    min-width: 0;
}
.log-heading {
    display: none;
}
.log-entry {
    border-bottom: 1px solid color-mix(in srgb, var(--primary-content) 10%, transparent);
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
    font-size: 0.8125rem;
    line-height: 1.6;
}
.log-entry:hover {
    background: color-mix(in srgb, var(--accent) 4%, transparent);
}
.log-time {
    grid-area: time;
    white-space: nowrap;
    font-variant-numeric: tabular-nums;
}
.log-level-cell {
    grid-area: level;
}
.log-level {
    --log-tone: var(--accent);
    display: inline-block;
    padding: 0.125rem 0.5rem;
    border: 1px solid color-mix(in srgb, var(--log-tone) 35%, transparent);
    border-radius: 0.375rem;
    background: color-mix(in srgb, var(--log-tone) 12%, var(--primary));
    color: color-mix(in srgb, var(--primary-content) 75%, var(--log-tone));
    font-size: 0.6875rem;
    font-weight: 600;
    white-space: nowrap;
}
.log-level-information {
    --log-tone: #22c55e;
}
.log-level-warning {
    --log-tone: #f59e0b;
}
.log-level-error {
    --log-tone: #ef4444;
}
.log-source {
    grid-area: source;
    color: color-mix(in srgb, var(--primary-content) 70%, var(--syntax-key));
    overflow-wrap: anywhere;
}
.log-message {
    grid-area: message;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}
.log-message :deep(.log-highlight-green) {
    color: color-mix(in srgb, var(--primary-content) 65%, #22c55e);
}
.log-message :deep(.log-highlight-red) {
    color: var(--danger);
}
.log-message :deep(.log-highlight-orange) {
    color: color-mix(in srgb, var(--primary-content) 65%, #f59e0b);
}
.log-stack {
    margin: 0 1rem 0.75rem;
    padding: 0.75rem;
    border-radius: 0.375rem;
    background: var(--tertiary);
    font-size: 0.75rem;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}
.logs-empty {
    padding: 4rem 1rem;
    text-align: center;
}
.logs-footer {
    border-radius: 0 0 0.75rem 0.75rem;
    border-top: 1px solid color-mix(in srgb, var(--primary-content) 15%, transparent);
}
@container logs (min-width: 40rem) {
    .log-grid {
        grid-template-columns: 5rem 7.5rem minmax(0, 1fr) minmax(0, 2fr);
        grid-template-areas: 'time level source message';
    }
    .log-heading {
        display: grid;
        position: sticky;
        top: 0;
        z-index: 1;
        background: var(--tertiary);
        border-bottom: 1px solid color-mix(in srgb, var(--primary-content) 20%, transparent);
        font-size: 0.875rem;
        font-weight: 600;
    }
}
</style>
