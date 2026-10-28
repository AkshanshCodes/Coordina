import { ArrowLeft, BookOpenText, Gear, Kanban } from "@phosphor-icons/react"
import { useCallback, useEffect, useState } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { ApiError } from "@/lib/api"
import { useAuth } from "@/features/auth/auth-context"
import { ProjectIconMark } from "@/features/projects/components/project-personalization"
import { getProject } from "@/features/projects/project-api"
import type { Project } from "@/types/project"
import { ProjectBoard } from "@/features/board/components/project-board"
import { useWorkspaces } from "@/features/workspaces/workspace-context"

type ProjectShellProps = {
  projectId: string
  onBack: () => void
}

type ProjectTab = "board" | "docs" | "settings"

const tabs: Array<{
  id: ProjectTab
  label: string
  icon: React.ComponentType<{ className?: string }>
}> = [
  { id: "board", label: "Board", icon: Kanban },
  { id: "docs", label: "Docs", icon: BookOpenText },
  { id: "settings", label: "Settings", icon: Gear },
]

export function ProjectShell({ projectId, onBack }: ProjectShellProps) {
  const { session, signOut } = useAuth()
  const { activeWorkspace, activeWorkspaceId } = useWorkspaces()
  const [activeTab, setActiveTab] = useState<ProjectTab>("board")
  const [project, setProject] = useState<Project | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const loadProject = useCallback(async () => {
    if (!session || !activeWorkspaceId) {
      setProject(null)
      return
    }

    setIsLoading(true)
    setError(null)

    try {
      setProject(
        await getProject(session.accessToken, activeWorkspaceId, projectId)
      )
    } catch (requestError) {
      if (requestError instanceof ApiError) {
        setError(requestError.message)

        if (requestError.status === 401) {
          signOut()
        }
      } else {
        setError("Unable to load project.")
      }
    } finally {
      setIsLoading(false)
    }
  }, [activeWorkspaceId, projectId, session, signOut])

  useEffect(() => {
    queueMicrotask(() => {
      setActiveTab("board")
      setProject(null)
      void loadProject()
    })
  }, [loadProject])

  if (!activeWorkspace || !activeWorkspaceId) {
    return null
  }

  if (isLoading) {
    return <ProjectShellSkeleton />
  }

  if (error || !project) {
    return (
      <Card className="bg-white/82 shadow-[0_24px_70px_rgba(24,24,27,0.08)] backdrop-blur-xl dark:bg-white/[0.055]">
        <CardHeader>
          <CardTitle>Project unavailable</CardTitle>
          <CardDescription>
            {error ??
              "This project could not be found in the active workspace."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            type="button"
            variant="outline"
            className="h-9 rounded-md px-3"
            onClick={onBack}
          >
            <ArrowLeft className="size-4" />
            Back to projects
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="grid gap-2">
      <section className="rounded-md border border-zinc-950/[0.08] bg-white/84 px-3 py-2 shadow-[0_12px_28px_rgba(24,24,27,0.06)] backdrop-blur-xl dark:border-white/10 dark:bg-white/[0.055]">
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-8 rounded-md text-muted-foreground"
            onClick={onBack}
          >
            <ArrowLeft className="size-4" />
          </Button>
          <ProjectIconMark icon={project.icon} color={project.color} />
          <div className="min-w-0 flex-1">
            <div className="flex min-w-0 flex-wrap items-center gap-2">
              <p className="truncate text-sm font-semibold text-zinc-950 dark:text-white">
                {project.name}
              </p>
              {project.key ? (
                <Badge className="h-5 rounded-sm px-1.5 font-mono text-[10px]">
                  {project.key}
                </Badge>
              ) : null}
              <StatusBadge status={project.status} />
            </div>
          </div>
          <nav className="flex gap-1" aria-label="Project modules">
            {tabs.map((tab) => {
              const Icon = tab.icon

              return (
                <Button
                  key={tab.id}
                  type="button"
                  variant={activeTab === tab.id ? "default" : "ghost"}
                  className="h-8 rounded-md px-2 text-xs"
                  aria-pressed={activeTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                >
                  <Icon className="size-3.5" />
                  {tab.label}
                </Button>
              )
            })}
          </nav>
        </div>
      </section>

      <div>
        {activeTab === "board" ? (
          <ProjectBoard project={project} workspace={activeWorkspace} />
        ) : activeTab === "docs" ? (
          <PlaceholderModule
            icon={BookOpenText}
            title="Project docs"
            description="Structured project knowledge will live here."
          />
        ) : (
          <PlaceholderModule
            icon={Gear}
            title="Project settings"
            description="Configuration will stay separate from delivery work."
          />
        )}
      </div>
    </div>
  )
}

function PlaceholderModule({
  description,
  icon: Icon,
  title,
}: {
  description: string
  icon: React.ComponentType<{ className?: string }>
  title: string
}) {
  return (
    <div className="grid min-h-72 place-items-center rounded-md border border-dashed border-zinc-950/15 bg-white/54 px-4 py-12 text-center dark:border-white/15 dark:bg-white/[0.035]">
      <div className="max-w-md">
        <span className="mx-auto grid size-11 place-items-center rounded-2xl bg-zinc-950 text-white dark:bg-white dark:text-zinc-950">
          <Icon className="size-5" />
        </span>
        <h2 className="mt-4 text-base font-semibold">{title}</h2>
        <p className="mt-2 text-sm leading-6 text-muted-foreground">
          {description}
        </p>
      </div>
    </div>
  )
}

function ProjectShellSkeleton() {
  return (
    <div className="grid gap-2">
      <section className="rounded-md border border-zinc-950/[0.08] bg-white/84 px-3 py-2 shadow-[0_12px_28px_rgba(24,24,27,0.06)] backdrop-blur-xl dark:border-white/10 dark:bg-white/[0.055]">
        <Skeleton className="h-8 w-80 max-w-full" />
      </section>
      <Card className="bg-white/82 dark:bg-white/[0.055]">
        <CardContent className="grid gap-4 p-5">
          <Skeleton className="h-12 w-full" />
          <div className="grid gap-2 xl:grid-cols-4">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

function StatusBadge({ status }: { status: Project["status"] }) {
  const className =
    status === "ACTIVE"
      ? "border-teal-500/20 bg-teal-500/10 text-teal-700 dark:text-teal-200"
      : status === "COMPLETED"
        ? "border-indigo-500/20 bg-indigo-500/10 text-indigo-700 dark:text-indigo-200"
        : "border-zinc-500/20 bg-zinc-500/10 text-zinc-700 dark:text-zinc-200"

  return <Badge className={className}>{status}</Badge>
}
