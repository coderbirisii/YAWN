import json
import os
import time
from playwright.sync_api import sync_playwright
from bs4 import BeautifulSoup

SAVE_DIR = r"C:\Users\Coder\Desktop\NOWT\NOWT\JSONS"
if not os.path.exists(SAVE_DIR):
    os.makedirs(SAVE_DIR)

ENDPOINTS = [
    "agents", "buddies", "bundles", "ceremonies", "competitivetiers",
    "contenttiers", "contracts", "currencies", "events", "flex", "gamemodes",
    "gear", "levelborders", "maps", "playercards", "playertitles",
    "seasons", "sprays", "themes", "weapons", "version"
]

def scrape_endpoint(endpoint):
    url = f"https://dash.valorant-api.com/endpoints/{endpoint}"
    
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        
        print(f"[{endpoint}] yükleniyor...")
        try:
            page.goto(url, wait_until="networkidle", timeout=60000)
            time.sleep(5)

            endpoints_data = {"endpoints": []}
            
            html = page.content()
            soup = BeautifulSoup(html, 'html.parser')
            
            container = soup.select_one(".mud-container")
            h4_elements = container.select(".mud-typography-h4")
            
            for h4 in h4_elements:
                method_elem = h4.select_one(".mud-chip-content")
                method = method_elem.get_text(strip=True) if method_elem else "GET"
                
                name = h4.get_text(strip=True).replace(method, "").strip()
                
                api_url = ""
                description = ""
                info = ""
                parameters = []
                responses = []
                
                sibling = h4.find_next_sibling()
                
                while sibling:
                    if sibling.name == "h4":
                        break
                    
                    if sibling.name == "hr":
                        break
                    
                    if sibling.name == "a" and "mud-link" in sibling.get("class", []):
                        api_url = sibling.get_text(strip=True)
                    
                    elif sibling.name == "p" and "mud-typography-body1" in sibling.get("class", []):
                        inner_p = sibling.find("p")
                        if inner_p:
                            info = inner_p.get_text(" ", strip=True)
                        elif not description:
                            description = sibling.get_text(" ", strip=True)
                    
                    elif sibling.name == "div" and "mud-table" in sibling.get("class", []):
                        rows = sibling.select("tbody tr")
                        for row in rows:
                            cells = row.select("td")
                            if len(cells) >= 5:
                                parameters.append({
                                    "name": cells[0].get_text(strip=True).replace("`", ""),
                                    "type": cells[1].get_text(strip=True).replace("`", ""),
                                    "description": cells[2].get_text(strip=True),
                                    "default": cells[3].get_text(strip=True).replace("`", ""),
                                    "required": cells[4].get_text(strip=True).lower() == "yes"
                                })
                    
                    elif sibling.name == "div" and "mud-tabs" in sibling.get("class", []):
                        tabs = sibling.select(".mud-tab")
                        for i, tab in enumerate(tabs):
                            tab_text = tab.get_text(strip=True)
                            if tab_text.isdigit():
                                status_code = int(tab_text)
                                
                                page_tabs = page.query_selector_all(".mud-tab")
                                if i < len(page_tabs):
                                    page_tabs[i].click()
                                    time.sleep(1)
                                
                                updated_html = page.content()
                                updated_soup = BeautifulSoup(updated_html, 'html.parser')
                                
                                schema = []
                                panels = updated_soup.select(".mud-tabs-panels .mud-table")
                                if panels:
                                    rows = panels[-1].select("tbody tr")
                                    for row in rows:
                                        cells = row.select("td")
                                        if cells:
                                            schema.append(cells[0].get_text(" ", strip=True))
                                
                                desc = "Success" if status_code == 200 else "Bad Request" if status_code == 400 else "Not Found"
                                responses.append({
                                    "status_code": status_code,
                                    "description": desc,
                                    "schema": schema
                                })
                    
                    sibling = sibling.find_next_sibling()
                
                if not responses:
                    responses = [{"status_code": 200, "description": "Success"}]
                
                endpoint_info = {
                    "method": method,
                    "name": name,
                    "url": api_url,
                    "description": description,
                    "parameters": parameters,
                    "responses": responses
                }
                
                if info:
                    endpoint_info["info"] = info
                
                endpoints_data["endpoints"].append(endpoint_info)
            
            file_path = os.path.join(SAVE_DIR, f"{endpoint}.json")
            with open(file_path, "w", encoding="utf-8") as f:
                json.dump(endpoints_data, f, indent=4, ensure_ascii=False)
            
            print(f"--- BAŞARILI: {endpoint}.json kaydedildi. ({len(endpoints_data['endpoints'])} endpoint)")
            
        except Exception as e:
            print(f"!!! HATA [{endpoint}]: {e}")
        
        finally:
            browser.close()

for ep in ENDPOINTS:
    scrape_endpoint(ep)
    time.sleep(1)

print("\nTüm endpoint'ler tamamlandı!")
