import { describe, it, expect } from '@jest/globals'
import Modal from '../../components/Modal.vue'
import { mount } from '@vue/test-utils'

describe('Modal.vue', () => {
    it('does not render when isOpen is false', () => {
        const wrapper = mount(Modal, {
            props: {
                isOpen: false,
                message: 'Test message'
            },
            global: {
                stubs: {
                    Teleport: true,
                    Transition: true
                }
            }
        })
        expect(wrapper.find('.fixed').exists()).toBe(false)
    })

    it('renders message and title when isOpen is true', () => {
        const wrapper = mount(Modal, {
            props: {
                isOpen: true,
                title: 'Test Title',
                message: 'Test message'
            },
            global: {
                stubs: {
                    Teleport: true,
                    Transition: true
                }
            }
        })
        expect(wrapper.text()).toContain('Test Title')
        expect(wrapper.text()).toContain('Test message')
    })

    it('emits close and cancel when close button is clicked', async () => {
        const wrapper = mount(Modal, {
            props: {
                isOpen: true,
                message: 'Test message',
                type: 'info'
            },
            global: {
                stubs: {
                    Teleport: true,
                    Transition: true
                }
            }
        })

        const closeButton = wrapper.find('button')
        await closeButton.trigger('click')

        expect(wrapper.emitted()).toHaveProperty('close')
        expect(wrapper.emitted()).toHaveProperty('cancel')
    })

    it('renders confirm and cancel buttons for type="confirm"', () => {
        const wrapper = mount(Modal, {
            props: {
                isOpen: true,
                message: 'Test message',
                type: 'confirm',
                confirmText: 'Yes',
                cancelText: 'No'
            },
            global: {
                stubs: {
                    Teleport: true,
                    Transition: true
                }
            }
        })

        const buttons = wrapper.findAll('button')
        expect(buttons).toHaveLength(2)
        expect(buttons[0].text()).toBe('No')
        expect(buttons[1].text()).toBe('Yes')
    })
})
