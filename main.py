import subprocess
import re

command = 'yt-dlp'
options = ''

def expand_urls(url):
    """
    URL の範囲指定を展開する。

    Parameters
    ----------
    url : str
        範囲指定された URL

    Returns
    -------
    expanded_urls : list
        展開された URL のリスト
    """
    match = re.search(r'\[(\d+)-(\d+)\]', url)
    if not match:
        return [url]

    start, end = map(int, match.groups())
    expanded_urls = []
    for i in range(start, end + 1):
        expanded_urls.append(re.sub(r'\[\d+-\d+\]', str(i), url))
    return expanded_urls

def get_urls():
    """
    url.txt から取得対象の URL 一覧を取得する。

    Returns
    -------
    url_list : list
        URL のリスト
    """
    url_list = []
    with open('url.txt', 'r') as f:
        for line in f:
            line = line.rstrip()
            url_list.extend(expand_urls(line))
    return url_list

def download_video(url, opt):
    """
    動画をダウンロードする。

    Parameters
    ----------
    url : str
        動画のURL
    opt : str
        オプション

    Returns
    -------
    None
    """
    cmd = f'{command} {url} {opt}'
    subprocess.run(cmd, shell=True)

# メイン処理
def main():
    url_list = get_urls()
    for url in url_list:
        download_video(url, options)

if __name__ == '__main__':
    main()
