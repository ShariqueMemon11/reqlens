import { FileText, GitCompare, TriangleAlert } from "lucide-react"

import { EmptyState } from "@/components/ui/empty-state"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export function IssueList() {
  return (
    <Tabs defaultValue="requirements" className="gap-4">
      <TabsList className="grid w-full grid-cols-3 md:w-fit">
        <TabsTrigger value="requirements">Requirements</TabsTrigger>
        <TabsTrigger value="ambiguities">Ambiguities</TabsTrigger>
        <TabsTrigger value="contradictions">Contradictions</TabsTrigger>
      </TabsList>

      <TabsContent value="requirements">
        <EmptyState
          icon={<FileText className="size-8" />}
          title="No requirements yet"
          description="Upload a PDF or paste text to extract discrete requirement units."
        />
      </TabsContent>

      <TabsContent value="ambiguities">
        <EmptyState
          icon={<TriangleAlert className="size-8" />}
          title="No ambiguities yet"
          description="Ambiguities show up here after a document is analyzed."
        />
      </TabsContent>

      <TabsContent value="contradictions">
        <EmptyState
          icon={<GitCompare className="size-8" />}
          title="No contradictions yet"
          description="Related requirements are compared after extraction. Conflicts appear here."
        />
      </TabsContent>
    </Tabs>
  )
}
