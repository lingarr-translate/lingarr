export interface IResource {
    currentDirectory: string
    parentDirectory: string
    subtitle: []
    list: IList[]
}

export interface IList {
    name: string
    type: string
}
