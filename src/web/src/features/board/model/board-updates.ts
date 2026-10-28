import type { Board, BoardCard } from "@/types/task"

export function moveCardInBoard(
  board: Board,
  cardId: string,
  listId: string
): Board {
  let movedCard: BoardCard | null = null

  const listsWithoutCard = board.lists.map((list) => {
    const cards = list.cards.filter((card) => {
      if (card.id === cardId) {
        movedCard = { ...card, listId }
        return false
      }

      return true
    })

    return { ...list, cards }
  })

  if (!movedCard) {
    return board
  }

  return {
    ...board,
    lists: listsWithoutCard.map((list) =>
      list.id === listId
        ? { ...list, cards: [...list.cards, movedCard as BoardCard] }
        : list
    ),
  }
}

export function removeCardFromBoard(board: Board, cardId: string): Board {
  return {
    ...board,
    lists: board.lists.map((list) => ({
      ...list,
      cards: list.cards.filter((card) => card.id !== cardId),
    })),
  }
}

export function renameListInBoard(
  board: Board,
  listId: string,
  title: string
): Board {
  return {
    ...board,
    lists: board.lists.map((list) =>
      list.id === listId ? { ...list, title } : list
    ),
  }
}
