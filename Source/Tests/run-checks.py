from pathlib import Path
import os,subprocess,json,time
repo=Path(__file__).resolve().parents[2];work=Path(os.environ.get('JAGO_COMPANION_TEST_WORK',str(repo/'Source/Tests/Run')))/time.strftime('%Y%m%d-%H%M%S');work.mkdir(parents=True)
si=subprocess.STARTUPINFO();si.dwFlags=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=0
p=subprocess.Popen([str(repo/'Jago-Companion.exe'),'--test',str(work)],env={**os.environ,'JAGO_SUITE_ROOT':str(work/'Suite')},startupinfo=si);p.wait(timeout=45);result=json.loads((work/'result.json').read_text());print(json.dumps(result,indent=2));assert result['ok'],result
