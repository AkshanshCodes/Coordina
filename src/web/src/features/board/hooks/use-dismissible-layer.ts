import { useEffect, useRef, useState } from "react"

export function useDismissibleLayer<T extends HTMLElement = HTMLDivElement>() {
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<T>(null)

  useEffect(() => {
    if (!isOpen) {
      return
    }

    function dismissOutside(target: EventTarget | null) {
      if (target instanceof Node && !containerRef.current?.contains(target)) {
        setIsOpen(false)
      }
    }

    function handlePointerDown(event: PointerEvent) {
      dismissOutside(event.target)
    }

    function handleFocusIn(event: FocusEvent) {
      dismissOutside(event.target)
    }

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") {
        return
      }

      setIsOpen(false)
      event.stopPropagation()
    }

    document.addEventListener("pointerdown", handlePointerDown, true)
    document.addEventListener("focusin", handleFocusIn, true)
    window.addEventListener("keydown", handleKeyDown, true)

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown, true)
      document.removeEventListener("focusin", handleFocusIn, true)
      window.removeEventListener("keydown", handleKeyDown, true)
    }
  }, [isOpen])

  return { containerRef, isOpen, setIsOpen }
}
