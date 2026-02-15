export const NODE_TYPE = {
    ROOT: 'root',
    STRING: 'string',
    NUMBER: 'number',
    BOOLEAN: 'boolean',
    OBJECT: 'object',
    ARRAY: 'array'
} as const

export type NodeType = (typeof NODE_TYPE)[keyof typeof NODE_TYPE]

export const NODE_TYPE_OPTIONS: TypeOption[] = [
    {
        value: NODE_TYPE.STRING,
        label: NODE_TYPE.STRING
    },
    {
        value: NODE_TYPE.NUMBER,
        label: NODE_TYPE.NUMBER
    },
    {
        value: NODE_TYPE.BOOLEAN,
        label: NODE_TYPE.BOOLEAN
    },
    {
        value: NODE_TYPE.OBJECT,
        label: NODE_TYPE.OBJECT
    },
    {
        value: NODE_TYPE.ARRAY,
        label: NODE_TYPE.ARRAY
    }
]

export interface TreeNode {
    id: number
    key: string
    type: NodeType
    value: string | number | boolean | null
    children: TreeNode[]
    expanded: boolean
}

export interface TypeOption {
    value: NodeType
    label: string
}
