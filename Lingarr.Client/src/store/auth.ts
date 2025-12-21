import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUser, IAuthStore } from '@/ts'
import services from '@/services'

export const useAuthStore = defineStore('auth', {
    state: (): IAuthStore => ({
        users: [],
        loading: false,
        error: '',
        success: '',
        isCreating: false,
        editingUserId: null,
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
        isCreatingUser: (state): boolean => state.isCreating,
        canSave: (state): boolean => {
            if (!state.isUsernameValid) {
                return false
            }

            // new users, password is required
            if (state.isCreating) {
                if (!state.editPassword || !state.isPasswordValid || !state.isConfirmPasswordValid) {
                    return false
                }
                if (state.editPassword !== state.editConfirmPassword) {
                    return false
                }
            }

            // existing users, password is optional
            if (state.editPassword || state.editConfirmPassword) {
                if (!state.isPasswordValid || !state.isConfirmPasswordValid) {
                    return false
                }
                if (state.editPassword !== state.editConfirmPassword) {
                    return false
                }
            }
            return true
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

        createOrEditUser(user?: IUser): void {
            this.isCreating = user === undefined
            this.editingUserId = user?.id ?? null
            this.editUsername = user?.username ?? ''
            this.isUsernameValid = user !== undefined
            this.isPasswordValid = user !== undefined
            this.isConfirmPasswordValid = user !== undefined
            this.editPassword = ''
            this.editConfirmPassword = ''
        },

        cancelEdit(): void {
            this.isCreating = false
            this.editingUserId = null
            this.editUsername = ''
            this.editPassword = ''
            this.editConfirmPassword = ''
            this.isUsernameValid = false
            this.isPasswordValid = true
            this.isConfirmPasswordValid = true
            this.error = ''
        },

        async saveUser(): Promise<void> {
            if (!this.canSave) return

            this.loading = true
            this.error = ''
            this.success = ''

            try {
                if (this.isCreating) {
                    // Creating a new user
                    await services.auth.signup({
                        username: this.editUsername,
                        password: this.editPassword
                    })
                    this.success = 'User created successfully'
                } else {
                    // Updating a user
                    await services.auth.updateUser(this.editingUserId!, {
                        username: this.editUsername,
                        password: this.editPassword,
                    })
                    this.success = 'User updated successfully'
                }

                setTimeout(() => {
                    this.success = ''
                }, 3000)

                this.cancelEdit()
                await this.loadUsers()
            } catch (err: any) {
                console.error('Error saving user:', err)
                this.error = err?.data?.message || (this.isCreating ? 'Failed to create user' : 'Failed to update user')
            } finally {
                this.loading = false
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
