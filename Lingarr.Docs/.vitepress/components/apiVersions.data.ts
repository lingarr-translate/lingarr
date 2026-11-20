import { defineLoader } from 'vitepress'

declare const data: string[]
export { data }

export default defineLoader({
    watch: ['../../public/api/definitions/*.json'],
    load(definitionFiles: string[]): string[] {
        return definitionFiles
            .map((definitionFile) => definitionFile.replace(/^.*[\\/]/, '').replace(/\.json$/, ''))
            .sort((first, second) => second.localeCompare(first, undefined, { numeric: true }))
    }
})
