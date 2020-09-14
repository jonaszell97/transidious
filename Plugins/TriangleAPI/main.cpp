
#include <llvm/Support/FileSystem.h>
#include <llvm/Support/Process.h>
#include <llvm/Support/Program.h>

#include <mutex>
#include <thread>

int main(int argc, const char **argv)
{
   int curr = 1;

   std::string triangle = "/usr/local/bin/triangle";

   std::mutex m;
   unsigned numThreads = std::thread::hardware_concurrency();

   std::vector<std::unique_ptr<std::thread>> threads;
   threads.reserve(numThreads);

   for (int i = 0; i < numThreads; ++i) {
      threads.emplace_back(std::make_unique<std::thread>([&]() {
         std::vector<llvm::StringRef> programArgs{ triangle, "-pPq0", "" };
         while (true) {
            {
               std::unique_lock<std::mutex> lock(m);
               if (curr >= argc) {
                  return;
               }

               programArgs[2] = argv[curr++];
            }

            int exitCode = llvm::sys::ExecuteAndWait(triangle, programArgs);
            if (exitCode != 0)
            {
               llvm::errs() << "triangle exited with code " << exitCode << ".\n";
            }
         }
      }));
   }

   for (auto &t : threads) {
      t->join();
   }

   return 0;
}
