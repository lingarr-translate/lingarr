import { Component } from 'vue'

export interface MenuItem {
    label: string
    icon: Component
    route: string
    children: string[]
}
