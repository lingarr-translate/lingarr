import { ref, watch } from 'vue'
import { NODE_TYPE, TreeNode, NodeType } from '@/ts'

export function useJsonTree(emit: (event: 'update:modelValue', value: string) => void) {
    const tree = ref<TreeNode[]>([])
    const parseError = ref<string | null>(null)
    let suppressWatch = false
    let nodeId = 0

    function nextId(): number {
        return nodeId++
    }

    function jsonToTree(obj: unknown, key = ''): TreeNode {
        const id = nodeId++

        if (obj === null) {
            return {
                id,
                key,
                type: NODE_TYPE.STRING,
                value: 'null',
                children: [],
                expanded: false
            }
        }

        if (Array.isArray(obj)) {
            return {
                id,
                key,
                type: NODE_TYPE.ARRAY,
                value: null,
                children: obj.map((item, index) => jsonToTree(item, String(index))),
                expanded: true
            }
        }

        if (typeof obj === 'object') {
            return {
                id,
                key,
                type: NODE_TYPE.OBJECT,
                value: null,
                children: Object
                    .entries(obj as Record<string, unknown>)
                    .map(([k, v]) => jsonToTree(v, k)
                ),
                expanded: true
            }
        }

        let type: NodeType = NODE_TYPE.STRING
        if (typeof obj === NODE_TYPE.NUMBER) {
            type = NODE_TYPE.NUMBER
        } else if (typeof obj === NODE_TYPE.BOOLEAN) {
            type = NODE_TYPE.BOOLEAN
        }

        return {
            id,
            key,
            type,
            value: obj as string | number | boolean,
            children: [],
            expanded: false
        }
    }

    function treeToJson(nodes: TreeNode[]): Record<string, unknown> {
        const result: Record<string, unknown> = {}
        for (const node of nodes) {
            const key = node.key || ''
            result[key] = nodeToValue(node)
        }

        return result
    }

    function nodeToValue(node: TreeNode): unknown {
        if (node.type === NODE_TYPE.OBJECT) {
            const obj: Record<string, unknown> = {}
            for (const child of node.children) {
                obj[child.key] = nodeToValue(child)
            }

            return obj
        }
        if (node.type === NODE_TYPE.ARRAY) {
            return node.children.map((child: any) => nodeToValue(child))
        }

        return node.value
    }

    function rebuildTree(jsonString: string) {
        try {
            const parsed = JSON.parse(jsonString)
            if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
                parseError.value = 'Template must be a JSON object'
                return
            }

            parseError.value = null
            tree.value = Object
                .entries(parsed)
                .map(([k, v]) => jsonToTree(v, k))
        } catch (e) {
            parseError.value = `Invalid JSON: ${(e as Error).message}`
        }
    }

    function onTreeChange() {
        const json = JSON.stringify(treeToJson(tree.value))
        suppressWatch = true
        emit('update:modelValue', json)
        requestAnimationFrame(() => {
            suppressWatch = false
        })
    }

    function addRootProperty() {
        tree.value.push({
            id: nodeId++,
            key: '',
            type: NODE_TYPE.STRING,
            value: '',
            children: [],
            expanded: false
        })
        onTreeChange()
    }

    function removeRoot(index: number) {
        tree.value.splice(index, 1)
        onTreeChange()
    }

    function watchModelValue(getModelValue: () => string) {
        watch(
            getModelValue,
            (value) => {
                if (!suppressWatch) {
                    rebuildTree(value)
                }
            },
            { immediate: true }
        )
    }

    return {
        tree,
        parseError,
        nextId,
        onTreeChange,
        addRootProperty,
        removeRoot,
        watchModelValue
    }
}
