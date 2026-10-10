<template>
    <span class="feature-icon" :data-kind="kind" aria-hidden="true">
        <svg viewBox="0 0 32 32" fill="none">
            <g v-if="kind === 'dashboard'">
                <rect x="5" y="5" width="9" height="13" rx="2.5" fill="#9ce9ff" />
                <rect x="18" y="5" width="9" height="8" rx="2.5" fill="#c4b5fd" />
                <rect x="5" y="22" width="9" height="5" rx="2" fill="#a5b4fc" />
                <rect x="18" y="17" width="9" height="10" rx="2.5" fill="#f0f9ff" />
            </g>
            <g v-else-if="kind === 'movies'">
                <rect x="4" y="11" width="24" height="16" rx="3" fill="#93c5fd" />
                <path d="M4 9a3 3 0 0 1 3-3l18-3a2 2 0 0 1 2 2l1 4-24 4V9Z" fill="#eff6ff" />
                <path d="m10 5 4 6M19 4l4 6" stroke="#3b82f6" stroke-width="3" />
                <path d="m14 16 7 4-7 4v-8Z" fill="#1e40af" />
                <path d="M7 26h18" stroke="#dbeafe" stroke-linecap="round" />
            </g>
            <g v-else-if="kind === 'shows'">
                <rect x="3" y="7" width="26" height="19" rx="4" fill="#ddd6fe" />
                <rect x="6" y="10" width="20" height="12" rx="2" fill="#7c3aed" />
                <path d="m7 21 7-10h11v10H7Z" fill="#c084fc" />
                <path d="m18 21 7-7v7h-7Z" fill="#f9a8d4" />
                <path
                    d="m12 29 1-3h6l1 3"
                    stroke="#a5b4fc"
                    stroke-width="2"
                    stroke-linecap="round" />
            </g>
            <g v-else-if="kind === 'translations'">
                <path
                    d="M5 4h13a3 3 0 0 1 3 3v10a3 3 0 0 1-3 3h-8l-6 4V7a3 3 0 0 1 1-3Z"
                    fill="#fde68a" />
                <path
                    d="m9 15 3-7 3 7m-5-2h4"
                    stroke="#92400e"
                    stroke-width="1.7"
                    stroke-linecap="round"
                    stroke-linejoin="round" />
                <path
                    d="M17 13h9a3 3 0 0 1 3 3v12l-5-3h-7a3 3 0 0 1-3-3v-6a3 3 0 0 1 3-3Z"
                    fill="#7dd3fc" />
                <path
                    d="M18 17h7m-4-2v2m2 0c0 4-2 5-5 6m1-4c1 2 3 3 6 4"
                    stroke="#075985"
                    stroke-width="1.5"
                    stroke-linecap="round" />
            </g>
            <g v-else-if="kind === 'files'">
                <path
                    d="M7 4h12l7 7v15a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2V6a2 2 0 0 1 1-2Z"
                    fill="#a7f3d0" />
                <path d="M19 4v7h7" fill="#6ee7b7" />
                <path
                    d="m11 19 3 3 7-7"
                    stroke="#047857"
                    stroke-width="2.5"
                    stroke-linecap="round"
                    stroke-linejoin="round" />
            </g>
            <g v-else-if="kind === 'lines'">
                <rect x="4" y="5" width="24" height="22" rx="4" fill="#fef3c7" />
                <path
                    d="M12 11h11m-11 5h11m-11 5h7"
                    stroke="#b45309"
                    stroke-width="2"
                    stroke-linecap="round" />
                <path
                    d="M8 11h.1M8 16h.1M8 21h.1"
                    stroke="#f59e0b"
                    stroke-width="3"
                    stroke-linecap="round" />
            </g>
            <g v-else-if="kind === 'characters'">
                <rect x="4" y="5" width="24" height="22" rx="4" fill="#fbcfe8" />
                <path
                    d="m8 22 5-12 5 12m-8-4h6"
                    stroke="#9d174d"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round" />
                <path
                    d="M22 15v7m0-6c-5-2-5 8 0 5"
                    stroke="#7c3aed"
                    stroke-width="2"
                    stroke-linecap="round" />
            </g>
            <g v-else>
                <path
                    d="m13 3 6 0 1 4 3 2 4-1 3 5-3 3v3l3 3-3 5-4-1-3 2-1 3h-6l-1-3-3-2-4 1-3-5 3-3v-3l-3-3 3-5 4 1 3-2 1-4Z"
                    transform="translate(1 -1) scale(.94)"
                    fill="#c4b5fd" />
                <circle cx="16" cy="16" r="7" fill="#7c3aed" />
                <circle cx="16" cy="16" r="4" fill="#a5f3fc" />
            </g>
        </svg>
    </span>
</template>

<script setup lang="ts">
defineProps<{ kind: string }>()
</script>
