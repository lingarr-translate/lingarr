export interface Statistics {
    totalLinesTranslated: number
    totalFilesTranslated: number
    totalCharactersTranslated: number
    totalMovies: number
    totalEpisodes: number
    totalSubtitles: number
    translationsByMediaType: Record<string, number>
    translationsByService: Record<string, number>
    subtitlesByLanguage: Record<string, number>
}
