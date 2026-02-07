<template>
    <CardComponent title="User Management">
        <template #description></template>
        <template #content>
            <div class="flex flex-col space-y-4">
                <StatusMessage :message="authStore.error" type="error" />
                <StatusMessage :message="authStore.success" type="success" />

                <div class="flex justify-end">
                    <ButtonComponent
                        variant="accent"
                        size="xs"
                        :disabled="authStore.isCreating || authStore.editingUserId !== null"
                        @click="authStore.createOrEditUser()">
                        Create User
                    </ButtonComponent>
                </div>

                <div v-if="authStore.loading" class="flex justify-center py-8">
                    <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                </div>

                <div >
                    <div class="border-accent grid grid-cols-12 border-b font-bold">
                        <div class="col-span-7 md:col-span-4 px-4 py-2">Username</div>
                        <div class="col-span-3 hidden md:block px-4 py-2">Last Login</div>
                        <div class="col-span-2 md:col-span-5 flex justify-end px-4 py-2">Actions</div>
                    </div>
                    <div v-if="authStore.isCreatingUser">
                        <div class="border-accent grid grid-cols-12 border-b hover:bg-accent/5 bg-accent/10">
                            <div class="col-span-7 md:col-span-4 px-4 py-2 flex items-center">
                                <div class="w-full">
                                    <InputComponent
                                        id="newUsername"
                                        v-model="authStore.editUsername"
                                        placeholder="Username"
                                        validation-type="string"
                                        :min-length="2"
                                        error-message="Username must be at least 2 characters long"
                                        @update:validation="authStore.setUsernameValidation" />
                                </div>
                            </div>
                            <div class="col-span-3 hidden md:flex items-center px-4 py-2 text-sm text-gray-400">
                                -
                            </div>
                            <div class="col-span-2 md:col-span-5 flex items-center justify-end gap-2 px-4 py-2">
                                <ButtonComponent
                                    variant="accent"
                                    size="xs"
                                    :disabled="authStore.loading"
                                    @click="authStore.cancelEdit">
                                    <span class="md:hidden">X</span>
                                    <span class="hidden md:block">Cancel</span>
                                </ButtonComponent>
                                <ButtonComponent
                                    variant="accent"
                                    size="xs"
                                    :disabled="!authStore.canSave || authStore.loading"
                                    :loading="authStore.loading"
                                    @click="authStore.saveUser()">
                                    Create
                                </ButtonComponent>
                            </div>
                        </div>
                        <div class="border-accent grid grid-cols-1 border-b bg-accent/5">
                            <div class="col-span-1 px-4 py-2">
                                <div class="space-y-2">
                                    <InputComponent
                                        id="newPassword"
                                        v-model="authStore.editPassword"
                                        type="password"
                                        label="Password (required)"
                                        placeholder="Enter password"
                                        validation-type="string"
                                        :min-length="4"
                                        error-message="Password must be at least 4 characters long and match"
                                        @update:validation="authStore.setPasswordValidation" />

                                    <InputComponent
                                        id="newConfirmPassword"
                                        v-model="authStore.editConfirmPassword"
                                        type="password"
                                        label="Confirm Password (required)"
                                        placeholder="Confirm your password"
                                        validation-type="string"
                                        :min-length="4"
                                        error-message="Password must be at least 4 characters long and match"
                                        @update:validation="authStore.setConfirmPasswordValidation" />
                                </div>
                            </div>
                        </div>
                    </div>
                    <!-- Existing Users -->
                    <div v-for="user in authStore.users" :key="user.id">
                        <div class="border-accent grid grid-cols-12 border-b hover:bg-accent/5">
                            <div class="col-span-7 md:col-span-4 px-4 py-2 flex items-center">
                                <div v-if="authStore.editingUserId === user.id" class="w-full">
                                    <InputComponent
                                        id="editUsername"
                                        v-model="authStore.editUsername"
                                        placeholder="Username"
                                        validation-type="string"
                                        :min-length="2"
                                        error-message="Username must be at least 2 characters long"
                                        @update:validation="authStore.setUsernameValidation" />
                                </div>
                                <span v-else class="text-sm">{{ user.username }}</span>
                            </div>
                            <div class="col-span-3 hidden md:flex items-center px-4 py-2 text-sm whitespace-nowrap">
                                {{ user.lastLoginAt ? formatDate(user.lastLoginAt) : 'Never' }}
                            </div>
                            <div class="col-span-2 md:col-span-5 flex items-center justify-end gap-2 px-4 py-2">
                                <template v-if="authStore.editingUserId === user.id">
                                    <ButtonComponent
                                        variant="accent"
                                        size="xs"
                                        :disabled="authStore.loading"
                                        @click="authStore.cancelEdit">
                                        <span class="md:hidden">X</span>
                                        <span class="hidden md:block">Cancel</span>
                                    </ButtonComponent>
                                    <ButtonComponent
                                        variant="accent"
                                        size="xs"
                                        :disabled="!authStore.canSave || authStore.loading"
                                        :loading="authStore.loading"
                                        @click="authStore.saveUser()">
                                        Save
                                    </ButtonComponent>
                                </template>
                                <template v-else>
                                    <ButtonComponent
                                        variant="accent"
                                        size="xs"
                                        @click="authStore.createOrEditUser(user)">
                                        Edit
                                    </ButtonComponent>
                                    <ButtonComponent
                                        v-if="authStore.users.length >= 2"
                                        variant="accent"
                                        size="xs"
                                        :disabled="authStore.deletingUserId === user.id"
                                        :loading="authStore.deletingUserId === user.id"
                                        @click="deleteUserConfirm(user)">
                                        Delete
                                    </ButtonComponent>
                                </template>
                            </div>
                        </div>
                        <div v-if="authStore.editingUserId === user.id" class="border-accent grid grid-cols-1 border-b bg-accent/5">
                            <div class="col-span-1 px-4 py-2">
                                <div class="space-y-2">
                                    <InputComponent
                                        id="editPassword"
                                        v-model="authStore.editPassword"
                                        type="password"
                                        label="New Password (optional)"
                                        placeholder="Leave blank to keep current password"
                                        validation-type="string"
                                        :min-length="4"
                                        error-message="Password must be at least 4 characters long and match"
                                        @update:validation="authStore.setPasswordValidation" />

                                    <InputComponent
                                        id="editConfirmPassword"
                                        v-model="authStore.editConfirmPassword"
                                        type="password"
                                        label="Confirm New Password (optional)"
                                        placeholder="Confirm your new password"
                                        validation-type="string"
                                        :min-length="4"
                                        error-message="Password must be at least 4 characters long and match"
                                        @update:validation="authStore.setConfirmPasswordValidation" />
                                </div>
                            </div>
                        </div>
                    </div>

                    <div if="authStore.users.length === 0 && !authStore.createUser" class="text-center text-gray-400 py-8">
                        No users found
                    </div>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { IUser } from '@/ts'
import CardComponent from '@/components/common/CardComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import StatusMessage from '@/components/common/StatusMessage.vue'
import { useAuthStore } from '@/store/auth'

const authStore = useAuthStore()

const formatDate = (dateString: string): string => {
    const date = new Date(dateString)
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
}

const deleteUserConfirm = async (user: IUser) => {
    if (!confirm(`Are you sure you want to delete user "${user.username}"?`)) {
        return
    }
    await authStore.deleteUser(user)
}

onMounted(async () => {
    await authStore.loadUsers()
})
</script>
