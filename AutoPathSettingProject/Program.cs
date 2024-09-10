using System.Reflection.Metadata.Ecma335;
using static System.Collections.Specialized.BitVector32;

namespace AutoPathSettingProject
{
    internal class Program
    {
        // 절대경로
        //static readonly string modOrganizerSettingFilePath = @"D:\Hachi Nakgo v1.1.3\Hachi Nakgo\ModOrganizer.ini";
        // 상대경로
        static readonly string modOrganizerSettingFilePath = @"..\ModOrganizer.ini";
        static readonly string falloutInstallPath = @"C:\Program Files (x86)\Steam\steamapps\common\Fallout 4";
        static readonly string targetSectionName = "customExecutables";
        static string fallOutInstallDrivePath = "";
        static string[] targetDrive = new string[] { 
          "C", "D", "E", "F" 
        };

        static void Main(string[] args)
        {
            List<string> targetTitleList;
            Dictionary<string, Dictionary<string, string>> iniData;

            try
            {
                // 0. 책임 안짐
                Console.WriteLine("ModOrganizer 자동 경로 설정을 진행합니다.");
                Console.WriteLine("[1] 현재 스팀 기본 설치 경로만 지원. (C:\\Program Files (x86)\\Steam\\steamapps\\common\\Fallout 4)");
                Console.WriteLine("[2] 지원 대상 : F4SE, FO4Edit 4.1.5f, FO4Edit 4.0.3, BodySlide x64, OutfitStudio x64, Fallout 4");
                Console.WriteLine("[3] 사용 전 ModOrganizer.ini 파일의 백업을 적극 권장합니다. 설정 다 날아가면 몰?루");
                Console.WriteLine("1 : 자동 경로 설정 진행, 2 : 종료  (1, 2 중 입력)");
                string startCheckString = Console.ReadLine().Trim();
                if(!startCheckString.Equals("1"))
                {
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }


                // 1. ModOrganizer.ini 파일 경로에 잘 넣었는지 확인
                try
                {
                    if (!FindModOrganizerSettingFile())
                    {
                        Console.WriteLine("INI 파일을 찾을 수 없음.");
                        Console.WriteLine("아무 키나 눌러 종료.");
                        Console.ReadKey();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR002 : ModOrganizer.exe 가 존재하는 경로에 프로그램 위치 요망.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }

                // 2. 경로 자동 세팅 할 대상 title 하드코딩.
                try 
                {
                    targetTitleList = BeforeSettingPath();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR003 : 여기서 에러가 발생하면 안됌. 이건 개발자 잘못임.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }

                // 3.  ModOrganizer.ini 파일 데이터를 읽어온다.
                try
                {
                    iniData = ReadIniFile(modOrganizerSettingFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR004 : ini 파일 읽는 중 오류 발생.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }

                // 4. ModOrganizer.ini 의 customExecutables 섹션 경로 탐색
                try
                {
                    UpdateModOrganizerCustomExecutablesSection(iniData, targetTitleList);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR005 : 경로 탐색 중  오류 발생.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }

                // 5. 탐색 완료한 경로 ModOrganizer.ini 에 수정 진행
                try
                {
                    UpdateModOrganizerIniFile(modOrganizerSettingFilePath, iniData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERR006 : 경로 탐색 중  오류 발생.");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("아무 키나 눌러 종료.");
                    Console.ReadKey();
                    return;
                }

                Console.WriteLine("경로 업데이트 완료.");
                Console.WriteLine("아무 키나 눌러 종료.");
                Console.ReadKey();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("땃... 모종의 이유로 실패함. 이하 실패 로그.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("아무 키나 눌러 종료.");
                Console.ReadKey();
                return;
            }
        }

        static bool CheckDrive()
        {
            Console.WriteLine("폴아웃4가 설치된 드라이브 입력 (대문자 C, D, E, F 중 하나만 입력)");
            fallOutInstallDrivePath = Console.ReadLine().Trim();

            if (!String.IsNullOrEmpty(fallOutInstallDrivePath))
            {
                if (!Array.Exists(targetDrive, drive => drive.Equals(fallOutInstallDrivePath, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("C, D, E, F 에 한해서만 지원함");
                    return false;
                }
                else
                {
                    try
                    {
                        DriveInfo drive = new DriveInfo(fallOutInstallDrivePath + @":\\");
                        if (drive.IsReady) return true;
                        else return false;
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("입력한 드라이브를 찾을 수 없음");
                        return false;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine(fallOutInstallDrivePath + "드라이브 접근 권한이 없음. 관리자 권한 실행 필요.");
                        return false;
                    }
                    catch (IOException)
                    {
                        Console.WriteLine(fallOutInstallDrivePath + "드라이브 접근 권한이 없음. 관리자 권한 실행 필요.");
                        return false;
                    }
                }
            }
            else
            {
                Console.WriteLine("C, D, E, F 에 한해서만 지원함");
                return false;
            }
        }

        static bool FindModOrganizerSettingFile()
        {
            bool check = File.Exists(modOrganizerSettingFilePath) ? true : false;

            return check;
        }

        static List<string> BeforeSettingPath()
        {
            List<string> returnList = new List<string> {
                "F4SE",
                "FO4Edit 4.1.5f",
                "FO4Edit 4.0.3",
                "BodySlide x64",
                "OutfitStudio x64",
                "Fallout 4"
            };

            return returnList;
        }

        static Dictionary<string, Dictionary<string, string>> ReadIniFile(string filePath)
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";

            foreach (var line in File.ReadAllLines(filePath))
            {
                string trimmedLine = line.Trim();

                // 빈 줄 또는 주석 무시
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]")) // 섹션 발견
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2).Trim();
                    if (!iniData.ContainsKey(currentSection))
                    {
                        iniData[currentSection] = new Dictionary<string, string>();
                    }
                }
                else if (trimmedLine.Contains("=")) // 키=값 형식 발견
                {
                    var keyValue = trimmedLine.Split(new char[] { '=' }, 2);
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();

                    if (!iniData.ContainsKey(currentSection))
                    {
                        iniData[currentSection] = new Dictionary<string, string>();
                    }

                    iniData[currentSection][key] = value;
                }
            }

            return iniData;
        }

        static void UpdateModOrganizerCustomExecutablesSection(Dictionary<string, Dictionary<string, string>> iniData, List<string> targetTitleList)
        {
            if (!iniData.ContainsKey(targetSectionName))
            {
                Console.WriteLine($"섹션 '{targetSectionName}'을 찾을 수 없음.");
                return;
            }

            var sectionData = iniData[targetSectionName];

            // index가 앞으로의 버전업으로 몇번까지 생길지 모르기 때문에 동적 처리 진행함. title은 하드코딩...
            int maxIndex = 0;
            foreach (var key in sectionData.Keys)
            {
                if (key.Contains("\\title"))
                {
                    // "1\title", "2\title" 형태에서 숫자 부분만 추출
                    string[] parts = key.Split('\\');
                    if (int.TryParse(parts[0], out int index))
                    {
                        maxIndex = Math.Max(maxIndex, index);
                    }
                }
            }

            // 하드코딩된 타이틀 명 탐색 진행
            foreach (var title in targetTitleList)
            {
                for (int i = 1 ; i <= maxIndex ; i++)
                {
                    string titleKey = $"{i}\\title";
                    string workingDirKey = $"{i}\\workingDirectory";
                    string binaryKey = $"{i}\\binary";
                    bool binaryCheck = false;

                    if (sectionData.ContainsKey(titleKey) && sectionData[titleKey] == title)
                    {
                        if (sectionData.ContainsKey(binaryKey))
                        {
                            sectionData[binaryKey] = AutoBinaryPathSetting(title, sectionData[binaryKey]);
                            binaryCheck = true;
                        }

                        if (sectionData.ContainsKey(workingDirKey))
                        {
                            if(binaryCheck)
                            {
                                sectionData[workingDirKey] = Path.GetDirectoryName(sectionData[binaryKey]).Replace(@"\", "/");
                            }
                        }
                    }
                }
            }
        }

        // 쌩노가다 시작
        static string AutoBinaryPathSetting(string title, string orgBinaryPath)
        {
            string returnPath = "";
            string findPath = "";

            switch (title)
            {
                case "F4SE":
                    findPath = falloutInstallPath;

                    if (File.Exists(findPath + @"\f4se_loader.exe"))
                    {
                        returnPath = Path.GetFullPath(findPath + @"\f4se_loader.exe").Replace(@"\", "/");
                    }
                    else
                    {
                        returnPath = orgBinaryPath;
                    }

                    break;

                case "FO4Edit 4.1.5f":
                    findPath = @"..\Tools\FO4Edit 4.1.5f\FO4Edit.exe";

                    if(File.Exists(findPath))
                    {
                        @returnPath = Path.GetFullPath(findPath).Replace(@"\", "/"); ;
                    }
                    else
                    {
                        returnPath = orgBinaryPath;
                    }

                    break;

                case "FO4Edit 4.0.3":
                    findPath = @"..\Tools\FO4Edit 4.0.3\FO4Edit.exe";

                    if (File.Exists(findPath))
                    {
                        returnPath = Path.GetFullPath(findPath).Replace(@"\", "/"); ;
                    }
                    else
                    {
                        returnPath = orgBinaryPath;
                    }

                    break;

                case "BodySlide x64":
                    findPath = @"..\mods\";
                    string bodySliderFolderName = "";

                    string[] bodySliderDirectories = Directory.GetDirectories(findPath, "*", SearchOption.TopDirectoryOnly);

                    foreach (string dir in bodySliderDirectories)
                    {
                        // 폴더 이름에 searchTerm이 포함되어 있는지 확인
                        if (dir.Contains("BodySlide and Outfit Studio"))
                        {
                            bodySliderFolderName = Path.GetFileName(dir);
                        }
                    }

                    if(String.IsNullOrEmpty(bodySliderFolderName))
                    {
                        returnPath = orgBinaryPath;
                    }
                    else
                    {
                        findPath = findPath + bodySliderFolderName + @"\Tools\BodySlide\BodySlide x64.exe";

                        if (File.Exists(findPath))
                        {
                            returnPath = Path.GetFullPath(findPath).Replace(@"\", "/"); ;
                        }
                        else
                        {
                            returnPath = orgBinaryPath;
                        }
                    }

                    break;

                case "OutfitStudio x64":
                    findPath = @"..\mods\";
                    string outpitStudioFolderName = "";

                    string[] outpitStudioDirectories = Directory.GetDirectories(findPath, "*", SearchOption.TopDirectoryOnly);

                    foreach (string dir in outpitStudioDirectories)
                    {
                        if (dir.Contains("BodySlide and Outfit Studio"))
                        {
                            outpitStudioFolderName = Path.GetFileName(dir);
                        }
                    }

                    if (String.IsNullOrEmpty(outpitStudioFolderName))
                    {
                        returnPath = orgBinaryPath;
                    }
                    else
                    {
                        findPath = findPath + outpitStudioFolderName + @"\Tools\BodySlide\OutfitStudio x64.exe";

                        if(File.Exists(findPath))
                        {
                            returnPath = Path.GetFullPath(findPath).Replace(@"\", "/"); ;
                        }
                        else
                        {
                            returnPath = orgBinaryPath;
                        }
                    }

                    break;

                case "Fallout 4":
                    findPath = falloutInstallPath;

                    if (File.Exists(findPath + @"\Fallout4.exe"))
                    {
                        returnPath = Path.GetFullPath(findPath + @"\Fallout4.exe").Replace(@"\", "/");
                    }
                    else
                    {
                        returnPath = orgBinaryPath;
                    }

                    break;

                default:
                    returnPath = orgBinaryPath;
                    break;
            }

            return returnPath;
        }

        static string SearchFolder(string directory, string folderPathToFind)
        {
            try
            {
                // 현재 디렉토리의 서브 폴더들 검색
                foreach (string folder in Directory.GetDirectories(directory))
                {
                    try
                    {
                        // 현재 폴더의 경로와 찾고자 하는 폴더 경로를 결합
                        string currentFolderPath = Path.GetRelativePath(directory, folder);

                        if (currentFolderPath.Equals(folderPathToFind, StringComparison.OrdinalIgnoreCase))
                        {
                            return folder; // 절대 경로 반환
                        }

                        // 재귀적으로 서브 폴더 검색
                        string foundPath = SearchFolder(folder, folderPathToFind);
                        if (!String.IsNullOrEmpty(foundPath))
                        {
                            return foundPath; // 절대 경로 반환
                        }
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.WriteLine(ex.Message);
                        // 접근 권한이 없으면 무시하고 넘어감
                    }
                    catch (PathTooLongException ex)
                    {
                        Console.WriteLine(ex.Message);
                        // 경로가 너무 길면 무시하고 넘어감
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return ""; // 폴더를 찾지 못했을 경우
        }

        // 수정된 INI 파일 다시 쓰기
        static void UpdateModOrganizerIniFile(string filePath, Dictionary<string, Dictionary<string, string>> iniData)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (var section in iniData)
                {
                    writer.WriteLine($"[{section.Key}]");
                    //Console.WriteLine($"디버그1 : [{section.Key}]");

                    foreach (var keyValue in section.Value)
                    {
                        writer.WriteLine($"{keyValue.Key} = {keyValue.Value}");
                        //Console.WriteLine($"디버그2 : {keyValue.Key} = {keyValue.Value}");
                    }

                    writer.WriteLine(); // 섹션 간 줄바꿈
                }
            }
        }
    }
}