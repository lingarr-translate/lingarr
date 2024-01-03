export interface IResource {
    name: string
    subtitles: []
}

export interface IResourceState {
    filter: string
    mediaType: string
    resource: IResource[]
}
