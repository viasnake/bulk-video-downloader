import subprocess

exe_file = 'yt-dlp.exe'
options = ''

def read_url():
    """
    url.txtからURLを読み込む
    
    Returns
    -------
    url_list : list
        URLのリスト
    """
    url_list = []
    with open('url.txt') as f:
        for line in f:
            url_list.append(line.rstrip())
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
    cmd = f'{exe_file} {url} {opt}'
    subprocess.run(cmd, shell=True)

# メイン処理
def main():
    url_list = read_url()
    for url in url_list:
        download_video(url, options)

if __name__ == '__main__':
    main()
