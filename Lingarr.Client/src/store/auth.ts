import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUser, IAuthStore } from '@/ts'
import services from '@/services'

export const useAuthStore = defineStore('auth', {
    state: (): IAuthStore => ({
        users: [],
        loading: false,
        error: '',
        success: '',
        editingUserId: null,
        savingUserId: null,
        deletingUserId: null,
        editUsername: '',
        editPassword: '',
        editConfirmPassword: '',
        isUsernameValid: false,
        isPasswordValid: true,
        isConfirmPasswordValid: true
    }),
    getters: {
        getUsers: (state): IUser[] => state.users,
        canSave: (state): boolean => {
            if (!state.isUsernameValid) {
                return false
            }

            if (state.editPassword || state.editConfirmPassword) {
                if (!state.isPasswordValid || !state.isConfirmPasswordValid) {
                    return false
                }
                if (state.editPassword !== state.editConfirmPassword) {
                    return false
                }
            }
            return true
        },
        passwordMatchError: (state): string => {
            if (state.editPassword !== state.editConfirmPassword && (state.editPassword || state.editConfirmPassword)) {
                return 'Passwords do not match'
            }
            return 'Password must be at least 4 characters long'
        }
    },
    actions: {
        async loadUsers(): Promise<void> {
            this.loading = true
            this.error = ''

            try {
                this.users = await services.auth.getUsers()
            } catch (err: any) {
                console.error('Error loading users:', err)
                this.error = err?.data?.message || 'Failed to load users'
            } finally {
                this.loading = false
            }
        },

        startEdit(user: IUser): void {
            this.editingUserId = user.id
            this.editUsername = user.username
            this.editPassword = ''
            this.editConfirmPassword = ''
            this.isUsernameValid = true
            this.isPasswordValid = true
            this.isConfirmPasswordValid = true
        },

        cancelEdit(): void {
            this.editingUserId = null
            this.editUsername = ''
            this.editPassword = ''
            this.editConfirmPassword = ''
            this.isUsernameValid = false
            this.isPasswordValid = true
            this.isConfirmPasswordValid = true
            this.error = ''
        },

        async saveUser(userId: number): Promise<void> {
            if (!this.canSave) return

            this.savingUserId = userId
            this.error = ''
            this.success = ''

            try {
                const updateData: { username?: string; password?: string } = {}

                if (this.editUsername) {
                    updateData.username = this.editUsername
                }

                if (this.editPassword) {
                    updateData.password = this.editPassword
                }

                await services.auth.updateUser(userId, updateData)

                this.success = 'User updated successfully'
                setTimeout(() => {
                    this.success = ''
                }, 3000)

                this.cancelEdit()
                await this.loadUsers()
            } catch (err: any) {
                console.error('Error updating user:', err)
                this.error = err?.data?.message || 'Failed to update user'
            } finally {
                this.savingUserId = null
            }
        },

        async deleteUser(user: IUser): Promise<void> {
            this.deletingUserId = user.id
            this.error = ''
            this.success = ''

            try {
                await services.auth.deleteUser(user.id)

                this.success = 'User deleted successfully'
                setTimeout(() => {
                    this.success = ''
                }, 3000)

                await this.loadUsers()
            } catch (err: any) {
                console.error('Error deleting user:', err)
                this.error = err?.data?.message || 'Failed to delete user'
            } finally {
                this.deletingUserId = null
            }
        },

        setUsernameValidation(valid: boolean): void {
            this.isUsernameValid = valid
        },

        setPasswordValidation(valid: boolean): void {
            this.isPasswordValid = valid
        },

        setConfirmPasswordValidation(valid: boolean): void {
            this.isConfirmPasswordValid = valid
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useAuthStore, import.meta.hot))
}
